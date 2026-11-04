#!/bin/bash

function is_ready() {
  awslocal sns list-topics --query "Topics[?ends_with(TopicArn, ':trade_gateway_ched_updates')].TopicArn" || return 1
  awslocal sns list-topics --query "Topics[?ends_with(TopicArn, ':trade_gateway_docom_updates')].TopicArn" || return 1
  awslocal sns list-topics --query "Topics[?ends_with(TopicArn, ':trade_gateway_intra_updates')].TopicArn" || return 1

  awslocal sns get-topic-attributes \
    --topic-arn "arn:aws:sns:${AWS_REGION:-eu-west-2}:000000000000:trade_gateway_ched_attachments_internal.fifo" >/dev/null || return 1
  awslocal sqs get-queue-url --queue-name trade_gateway_ched_attachments_internal_downloader.fifo >/dev/null || return 1
  awslocal s3api head-bucket --bucket trade-gateway-documents || return 1
  awslocal s3api head-bucket --bucket cdp-uploader-quarantine || return 1
  awslocal sqs get-queue-url --queue-name cdp-uploader-scan-results-callback.fifo >/dev/null || return 1
  awslocal sqs get-queue-url --queue-name mock-clamav >/dev/null || return 1

  return 0
}

while ! is_ready; do
  echo "Waiting until ready..."
  sleep 1
done
