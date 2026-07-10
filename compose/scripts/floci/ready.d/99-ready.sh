#!/bin/bash

function is_ready() {
  awslocal sns list-topics --query "Topics[?ends_with(TopicArn, ':trade_gateway_ched_updates')].TopicArn" || return 1
  awslocal sns list-topics --query "Topics[?ends_with(TopicArn, ':trade_gateway_docom_updates')].TopicArn" || return 1
  awslocal sns list-topics --query "Topics[?ends_with(TopicArn, ':trade_gateway_intra_updates')].TopicArn" || return 1

  return 0
}

while ! is_ready; do
  echo "Waiting until ready..."
  sleep 1
done
