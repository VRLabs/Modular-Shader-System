const fs = require('fs');
const exec = require('child_process').execSync;

// Fetch command parameters

let gitRepo;
let destinationFolder;

if (!process.argv[2] && !process.argv[3]) {
    gitRepo = ".";
    destinationFolder = ".docsOutput";
}
else {
    gitRepo = process.argv[2];
    destinationFolder = process.argv[3] ?? ".";
}

if(gitRepo === ".") {
    destinationFolder = ".docsOutput";
}

var isSingleFolder = false;
if (!gitRepo.endsWith(".git")) {
    if(gitRepo !== ".") {
        console.log("warning: when not using a repository the current directory is automatically used instead, regardless of the value set");
        return;
    }
    isSingleFolder = true;
}

const destinationTagsFolder = destinationFolder + '/.t/tags';
const destinationMasterFolder = destinationFolder + '/.t/master';
const destinationDocsFolder = destinationFolder + '/docs';

createDirectory(destinationTagsFolder);
createDirectory(destinationMasterFolder);
createDirectory(destinationDocsFolder);

// Clone repo - copy folder
if (isSingleFolder) {
    console.log('Not a repo, copy folder');
    if(gitRepo === ".") {
        exec('ls -A | grep -v ".docsOutput" | xargs cp -r -t "' + destinationMasterFolder + '"');
    }
    else
        exec('cp -r  "' + gitRepo + '/." "' + destinationMasterFolder + '"');
}
else {
    console.log('Cloning repo');
    exec('git clone -q "' + gitRepo + '" "' + destinationMasterFolder + '"');
}

// Generate empty docs
console.log('Generating empty docs based on master');
exec('cp -r "' + destinationMasterFolder + '/.docs/." "' + destinationDocsFolder + '"');
exec('rm -r -f "' + destinationDocsFolder + '/.guides"');
exec('rm -r -f "' + destinationDocsFolder + '/.api"');
exec('rm -r -f "' + destinationDocsFolder + '/.src"');

const baseToc = fs.readFileSync(destinationDocsFolder + '/toc.yml', "utf-8");

var tags;
if (isSingleFolder) {
    tags = "\nDev".split("\n");
}
else {
    console.log('Retrieving Tags');
    exec('git -C "' + destinationMasterFolder + '" tag -l --sort=-version:refname v* > "' + destinationTagsFolder + '/tags.txt"');
    tags = fs.readFileSync(destinationTagsFolder + '/tags.txt', "utf-8").split("\n");
    tags.unshift("master");
}

const masterSrcFolder = destinationMasterFolder + '/Editor';
const masterApiFolder = destinationMasterFolder + '/.docs/api';
const masterGuidesFolder = destinationMasterFolder + '/.docs/guides';

var jsonFile = JSON.parse(fs.readFileSync(destinationDocsFolder + '/docfx.json'));

var lastBigTag = "nothing";
var currentlyIn = 0;

for (let i = 0; i < tags.length; i++) {
    if (currentlyIn > 5) break;
    if (!tags[i]) continue;
    if (tags[i].lastIndexOf(".") != -1 && tags[i].substring(0, tags[i].lastIndexOf(".")) === lastBigTag) continue;

    currentlyIn++;
    lastBigTag = tags[i].lastIndexOf(".") != -1 ? tags[i].substring(0, tags[i].lastIndexOf(".")) : "nothing";
    const tag = tags[i];
    const tagName = tag === "master" ? "Next" : tag;
    const tagFolder = i === 1 ? "" : tagName;
    const tagFolderSlashed = i === 1 ? "" : tagFolder + "/";

    var srcFolder = destinationDocsFolder + '/' + tagFolderSlashed + 'src';
    var apiFolder = destinationDocsFolder + '/' + tagFolderSlashed + 'api';
    var guidesFolder = destinationDocsFolder + '/' + tagFolderSlashed + 'guides';

    console.log('Copying content for tag ' + tag + ':');

    if (!isSingleFolder) {
        // Checkout to tag
        console.log('   Checkout');
        exec('git -C "' + destinationMasterFolder + '" checkout -q --no-guess ' + tag);
    }

    // Copy source code
    console.log('   Copy source code');
    createDirectory(srcFolder);

    exec('cp -r "' + masterSrcFolder + '/." "' + srcFolder + '"');

    // Create csproj for unity references
    fs.writeFileSync(srcFolder + '/ModularShaderSystem.csproj', '<Project Sdk="Microsoft.NET.Sdk">\n\n  <PropertyGroup>\n    <TargetFramework>.NETFramework,Version=v4.8</TargetFramework>\n  </PropertyGroup>\n\n  <ItemGroup>\n    <PackageReference Include="Unity3D.SDK" Version="2019.4.15.1" />\n  </ItemGroup>\n\n</Project>');
    exec('dotnet restore "' + srcFolder + '/ModularShaderSystem.csproj"');

    // Copy api content
    console.log('   Copy api folder');
    createDirectory(apiFolder);
    if (fs.existsSync(masterApiFolder))
        exec('cp -r "' + masterApiFolder + '/." "' + apiFolder + '"');
    else
        fs.writeFileSync(apiFolder + '/index.md', "# Welcome to the API Section\n\nHere you will find the documentation of each publicly available classes within the API.");

    // Copy guides content
    console.log('   Copy guides folder');
    createDirectory(guidesFolder);
    if (fs.existsSync(masterGuidesFolder))
        exec('cp -r "' + masterGuidesFolder + '/." "' + guidesFolder + '"');
    else
        fs.writeFileSync(guidesFolder + '/index.md', "Seems like at this point there weren't any guide available, try to look a more up to date version");

    // Add TOC
    console.log('   Add toc.yml');
    var tocFile = '- name: ' + tagName + '\n';
    tocFile += '  dropdown: true\n';
    tocFile += '  items:\n';

    var lastBigTagTOC = "nothing";
    var currentlyInTOC = 0;

    for (let j = 0; j < tags.length; j++) {
        if (currentlyInTOC > 5) break;
        if (!tags[j]) continue;
        if (tags[j].lastIndexOf(".") != -1 && tags[j].substring(0, tags[j].lastIndexOf(".")) === lastBigTagTOC) continue;

        currentlyInTOC++;
        lastBigTagTOC = tags[j].lastIndexOf(".") != -1 ? tags[j].substring(0, tags[j].lastIndexOf(".")) : "nothing";
        var internalTag = tags[j];
        tocFile += '    - name: ' + (internalTag === "master" ? "Next" : internalTag) + '\n';

        var ref = (i === 1 ? "" : "../");
        ref += internalTag === tags[1] ? "" : (internalTag === "master" ? "Next/" : internalTag + '/');
        tocFile += '      topicHref: ' + ref + 'api\n';
    }

    tocFile += baseToc;
    fs.writeFileSync(destinationDocsFolder + '/' + tagFolder + '/toc.yml', tocFile);

    // Update json
    console.log('   Add data to json');
    var srcentry = {
        src: [{ files: [tagFolder + (tagFolder ? "/" : "") + "src/**.csproj"] }],
        dest: tagFolder + (tagFolder ? "/" : "") + "api"
    }

    var contententry = {
        files: [
            "api/**.yml",
            "api/index.md",
            "guides/**.md",
            "guides/**.yml",
            "toc.yml",
            "*.md"
        ],
        src: tagFolder,
        dest: tagFolder,
        version: tagName,
        rootTocPath: tagFolder + (tagFolder ? "/" : "") + "toc.html"
    }

    jsonFile.metadata.push(srcentry);
    jsonFile.build.content.push(contententry);
}

// Save json
console.log('Saving docfx.json');
fs.writeFileSync(destinationDocsFolder + '/docfx.json', JSON.stringify(jsonFile, null, 2));

function createDirectory(dir) {
    if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
    }
}