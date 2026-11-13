#!/bin/bash
set -e

ACCOUNT=000000000000
REGION="${AWS_REGION:-eu-west-2}"

awslocal sns create-topic --name trade_gateway_ched_updates
awslocal sns create-topic --name trade_gateway_docom_updates
awslocal sns create-topic --name trade_gateway_intra_updates

# --- CHED attachment downloads ------------------------------------------------------------------
# GET certificates/cheds/{id}/attachments/{attachmentId} publishes to the FIFO topic (deduplicated on
# the attachment id); the downloader queue feeds the consumer that fetches from TRACES and hands the
# file to cdp-uploader. Mirrors trade-gateway-publisher's create_subscribed_queue.

TOPIC_ATTRIBUTES='{"FifoTopic":"true","ContentBasedDeduplication":"true"}'
DEADLETTER_ATTRIBUTES='{"FifoQueue":"true","ContentBasedDeduplication":"true"}'
SUBSCRIPTION_ATTRIBUTES='{"RawMessageDelivery":"true"}'

create_subscribed_queue() {
  local queue="$1" topic="$2" max_receive_count="$3" visibility_timeout="$4"
  local deadletter_name="$queue-deadletter.fifo"
  local queue_name="$queue.fifo"
  local topic_name="$topic.fifo"
  local queue_attributes
  queue_attributes="{\"FifoQueue\":\"true\",\"ContentBasedDeduplication\":\"true\",\"VisibilityTimeout\":\"$visibility_timeout\",\"RedrivePolicy\":\"{\\\"deadLetterTargetArn\\\":\\\"arn:aws:sqs:$REGION:$ACCOUNT:$deadletter_name\\\",\\\"maxReceiveCount\\\":\\\"$max_receive_count\\\"}\",\"Policy\":\"{\\\"Version\\\":\\\"2012-10-17\\\",\\\"Statement\\\": [{\\\"Effect\\\":\\\"Allow\\\",\\\"Principal\\\":{\\\"Service\\\":\\\"sns.amazonaws.com\\\"},\\\"Action\\\":\\\"sqs:SendMessage\\\",\\\"Resource\\\":\\\"arn:aws:sqs:$REGION:$ACCOUNT:$queue_name\\\",\\\"Condition\\\":{\\\"ArnEquals\\\":{\\\"aws:SourceArn\\\":\\\"arn:aws:sns:$REGION:$ACCOUNT:$topic_name\\\"}}}] }\" }"

  awslocal sqs create-queue --queue-name "$deadletter_name" --attributes "$DEADLETTER_ATTRIBUTES"
  awslocal sqs create-queue --queue-name "$queue_name" --attributes "$queue_attributes"
  awslocal sns subscribe --topic-arn "arn:aws:sns:$REGION:$ACCOUNT:$topic_name" \
    --protocol sqs \
    --notification-endpoint "arn:aws:sqs:$REGION:$ACCOUNT:$queue_name" \
    --attributes "$SUBSCRIPTION_ATTRIBUTES"
}

awslocal sns create-topic --name trade_gateway_ched_attachments_internal.fifo --attributes "$TOPIC_ATTRIBUTES"
create_subscribed_queue trade_gateway_ched_attachments_internal_downloader trade_gateway_ched_attachments_internal 3 120

# Scanned attachments. Expire after 40 days, 10 days after their Mongo record (30-day TTL index),
# so a record never points at an object that has already gone. In CDP the same rule is set on the
# bucket in cdp-tenant-config.
awslocal s3 mb s3://trade-gateway-documents || true # exists already when floci kept its data
awslocal s3api put-bucket-lifecycle-configuration --bucket trade-gateway-documents \
  --lifecycle-configuration '{"Rules":[{"ID":"expire-ched-attachments","Status":"Enabled","Filter":{"Prefix":""},"Expiration":{"Days":40}}]}'

# --- cdp-uploader -------------------------------------------------------------------------------
# What cdp-uploader needs locally (from DEFRA/cdp-uploader compose/01-start-localstack.sh): its
# quarantine bucket, scan result queues and the mock-clamav queue fed by quarantine bucket events.
awslocal s3 mb s3://cdp-uploader-quarantine || true # exists already when floci kept its data
awslocal sqs create-queue --queue-name cdp-clamav-results
awslocal sqs create-queue --queue-name cdp-uploader-scan-results-callback.fifo \
  --attributes '{"FifoQueue":"true","ContentBasedDeduplication":"true"}'
awslocal sqs create-queue --queue-name cdp-uploader-download-requests
awslocal sqs create-queue --queue-name mock-clamav
awslocal s3api put-bucket-notification-configuration --bucket cdp-uploader-quarantine \
  --notification-configuration "{\"QueueConfigurations\":[{\"QueueArn\":\"arn:aws:sqs:$REGION:$ACCOUNT:mock-clamav\",\"Events\":[\"s3:ObjectCreated:*\"]}]}"
