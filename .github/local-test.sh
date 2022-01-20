#! /bin/bash

rm -rf .docsOutput
node .github/prepare-docfx-folder
docfx .docsOutput/docs/docfx.json --serve
