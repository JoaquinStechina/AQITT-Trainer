#!/bin/sh
# Builds the Linux loader (libaqtloader.so) and the optional attach tool (aqt-inject). Needs gcc.
set -e
cd "$(dirname "$0")"
CC="${CC:-gcc}"
$CC -std=gnu11 -O2 -Wall -Wextra -fPIC -shared -o libaqtloader.so aqt_loader.c -ldl -lpthread
$CC -std=gnu11 -O2 -Wall -Wextra -o aqt-inject aqt_inject.c -ldl
echo "Built: $(pwd)/libaqtloader.so  $(pwd)/aqt-inject"
