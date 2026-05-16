#!/bin/bash
echo "Cleaning all bin and obj folders..."
find . -type d -name "bin" -exec rm -rf {} +
find . -type d -name "obj" -exec rm -rf {} +
echo "Done. Please restart the Aspire host now."
