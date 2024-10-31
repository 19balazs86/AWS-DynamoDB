#!/bin/bash

# Default values
STACK_NAME="Test-DynamoDB"
TEMPLATE_FILE="DynamoDB-template.yml"
# PARAMETERS="ParameterKey=QueueName,ParameterValue=TestQueue"

./AWS-Run-template.sh "$STACK_NAME" "$TEMPLATE_FILE"

# ./AWS-Run-template.sh "$STACK_NAME" "$TEMPLATE_FILE" "$PARAMETERS"
