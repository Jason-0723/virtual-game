#!/bin/sh
# 重建 Office.unity（Unity 要先關掉）：場景全部由 OfficeBuilder.cs 產生，改場景請改程式再跑這個
cd "$(dirname "$0")"
"/c/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe" -batchmode -quit -projectPath VirtualGame \
  -executeMethod OfficeBuilder.Build -logFile "$PWD/build.log"
code=$?
grep -E "error CS|Exception|\[Course\]" build.log | head -20
exit $code
