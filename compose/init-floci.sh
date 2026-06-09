#!/bin/bash

set -e

AWS_ENDPOINT="http://floci:4566"
REGION="eu-west-2"

TOPICS=(
  "trade_gateway_intra_updates"
  "trade_gateway_ched_updates"
  "trade_gateway_docom_updates"
)

echo "Creating SNS FIFO topics..."

for topic in "${TOPICS[@]}"; do
  topic_arn=$(aws --endpoint-url="$AWS_ENDPOINT" sns create-topic \
    --name "$topic" \
    --attributes FifoTopic=true,ContentBasedDeduplication=true \
    --region "$REGION" \
    --query 'TopicArn' \
    --output text)

  echo "Topic ARN: $topic_arn" # NOSONAR
done

is_ready() {
  for topic in "${TOPICS[@]}"; do
    aws --endpoint-url="$AWS_ENDPOINT" sns list-topics \
      --query "Topics[?ends_with(TopicArn, ':${topic}')].TopicArn" \
      >/dev/null || return 1
  done

  return 0
}

while ! is_ready; do
  echo "Waiting until ready"
  sleep 1
done

touch /tmp/ready