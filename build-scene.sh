#!/bin/sh
# 重建 Office.unity 和試玩關卡 Round1.unity（Unity 要先關掉）：場景由 OfficeBuilder.cs、RoundBuilder.cs 產生
cd "$(dirname "$0")"
"/c/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe" -batchmode -quit -projectPath VirtualGame \
  -executeMethod RoundBuilder.Build -logFile "$PWD/build.log"
code=$?
grep -E "error CS|Exception|\[Course\]" build.log | head -20
exit $code
