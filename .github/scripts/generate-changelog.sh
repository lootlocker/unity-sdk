#!/bin/bash
sdk=$1
sdkname=$2
openAIApiKey=$3

latestRelease=$(curl -s -X GET "https://api.github.com/repos/lootlocker/$sdk/releases/latest")

version=$(echo "$latestRelease" | jq .tag_name)
version=${version//\"/}
patchnotes=$(echo "$latestRelease" | jq .body)
patchnotes=${patchnotes//\\r\\n/\\n}

prompt="Condense these patch notes (but dont mention the version number) to a short message (max 255 character) naming the important changes and crucial information in a helpful, informative, but punchy way: $patchnotes"
prompt=${prompt//\"/}
body='{
        "model": "gpt-4o-mini",
        "messages": [
            {
                "role": "system",
                "content": "You are the chief tech communicator of LootLocker and a marketing Genius"
            },
            {
                "role": "user",
                "content": "'$prompt'"
            }
        ]
    }'

jsonResponse=$(curl -s "https://api.openai.com/v1/chat/completions" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $openAIApiKey" \
    -d "$body")

shortPatchNotes=$(echo "$jsonResponse" | jq .choices[0].message.content)
shortPatchNotes=${shortPatchNotes//\"/}

filename="$sdk-$version"
filename=${filename//./-}
filename="$filename.md"
echo "---" > $filename
echo "title: \"$sdkname $version\"" >> $filename
echo "author: Erik Bylund" >> $filename
dt=`date '+%Y-%m-%dT%H:%M:%SZ'`
echo "date: $dt" >> $filename
echo "---" >> $filename

echo "" >> $filename

echo "**🎉 $sdkname $version is released 🎉**" >> $filename
echo "$shortPatchNotes" >> $filename
echo "" >> $filename
echo "[Find it here](https://github.com/lootlocker/$sdk/releases/tag/$version)" >> $filename