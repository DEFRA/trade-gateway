#!/bin/bash

awslocal sns create-topic --name trade_gateway_ched_updates
awslocal sns create-topic --name trade_gateway_docom_updates
awslocal sns create-topic --name trade_gateway_intra_updates
