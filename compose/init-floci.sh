#!/bin/bash

set -e

AWS_ENDPOINT="http://floci:4566"
REGION="eu-west-2"

INTRA_TOPIC_NAME="trade_gateway_intra_updates"
CHED_TOPIC_NAME="trade_gateway_ched_updates"
DOCOM_TOPIC_NAME="trade_gateway_docom_updates"

echo "Creating SNS FIFO topics..."

INTRA_TOPIC_ARN=$(aws --endpoint-url=$AWS_ENDPOINT sns create-topic \
  --name "$INTRA_TOPIC_NAME" \
  --attributes FifoTopic=true,ContentBasedDeduplication=true \
  --region $REGION \
  --query 'TopicArn' \
  --output text)

echo "Topic ARN: $INTRA_TOPIC_ARN"

CHED_TOPIC_ARN=$(aws --endpoint-url=$AWS_ENDPOINT sns create-topic \
  --name "$CHED_TOPIC_NAME" \
  --attributes FifoTopic=true,ContentBasedDeduplication=true \
  --region $REGION \
  --query 'TopicArn' \
  --output text)

echo "Topic ARN: $CHED_TOPIC_ARN"

DOCOM_TOPIC_ARN=$(aws --endpoint-url=$AWS_ENDPOINT sns create-topic \
  --name "$DOCOM_TOPIC_NAME" \
  --attributes FifoTopic=true,ContentBasedDeduplication=true \
  --region $REGION \
  --query 'TopicArn' \
  --output text)

echo "Topic ARN: $DOCOM_TOPIC_ARN"


function is_ready() {
    aws --endpoint-url=$AWS_ENDPOINT sns list-topics --query "Topics[?ends_with(TopicArn, ':${INTRA_TOPIC_NAME}')].TopicArn" || return 1
    aws --endpoint-url=$AWS_ENDPOINT sns list-topics --query "Topics[?ends_with(TopicArn, ':${CHED_TOPIC_NAME}')].TopicArn" || return 1
    aws --endpoint-url=$AWS_ENDPOINT sns list-topics --query "Topics[?ends_with(TopicArn, ':${DOCOM_TOPIC_NAME}')].TopicArn" || return 1
    return 0
}

while ! is_ready; do
    echo "Waiting until ready"
    sleep 1
done

touch /tmp/ready
