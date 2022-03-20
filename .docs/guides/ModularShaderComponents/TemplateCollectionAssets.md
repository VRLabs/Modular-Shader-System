---
uid: str-TemplateCollectionAssets
title: Template Collection Assets
---

# Template Collection Assets

This asset is a collection of multiple templates under a single file. It's not directly used in its entirety in any place in the library, you can use any single template inside this collection like it was its own asset.

Like the template asset it is just a text file where you can write shader code, but unlike a template asset you need to start each part of code with a `#T#TEMPLATE_NAME` to tell the asset everything after it is part of this specific template.

You can have multiple template keywords like the above, and every new one ends the previous template and starts the new one. The template name will be the name of that template keyword.

You can create a template collection asset by selecting the menu `Assets > Create > Shader > VRLabs > Modular Shader > Template Collection`.
