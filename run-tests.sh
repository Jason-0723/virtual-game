#!/bin/sh
# 自動試玩測試（Unity 要先關掉）：sh run-tests.sh [測試名稱篩選]
cd "$(dirname "$0")"
UNITY="/c/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe"
"$UNITY" -batchmode -projectPath VirtualGame -runTests -testPlatform EditMode \
  ${1:+-testFilter "$1"} -testResults "$PWD/test-results.xml" -logFile "$PWD/test.log"
code=$?
grep -o -E 'total="[0-9]+" passed="[0-9]+" failed="[0-9]+"' test-results.xml | head -1
grep -E "error CS|\[Playthrough\]" test.log
grep -A2 "<message>" test-results.xml | grep -v "child tests" | head -20
exit $code
