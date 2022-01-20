---
uid: adv-EmbeddingLibrary
title: Embedding The Library
---

# Embedding The Library

Embedding the library is the process of making a functional copy of the library for your exclusive use, making you able to export a unity package of your modular shader without worrying about the end user having to download the modular shader system by themselves, and also gives you control over what version of the modular shader system the shader is shipped with.

If you're planning to make a shader to publish it's important to do this as fist step, since there are going to be some differences applied to the embedded library compared to the base one.

Luckily for you there's an editor window dedicated to this, and can be found in `VRLabs > Modular Shader > Embed Library`.

## Embed Library Window

![window](/images/docs/AdvancedTopics/1.png)

The window contains some fields that are filled with the default values. Most of these values need to be changed based on your needs.

- **Namespace:** this will be the namespace of the embedded library, it has to differ from `VRLabs` since this one is already there, and it would cause compilation errors. The end namespace will always be `*YourInput*.ModularShaderSystem` (a preview is visible in the window)
- **Default variable keyword:** is the keyword used by default when no keywords are provided for variables. You can change it to whatever you want, or keep it like that.
- **Default code keyword:** is the keyword used by default when no keywords are provided for function code implementation. You can change it to whatever you want, or keep it like that.
- **Default properties keyword:** is the keyword used as an entry point for templates that target the property block when that option is enabled. You can change it to whatever you want, or keep it like that.
- **Resource folder:** it will be the name used for the resource folder of the library, it has to be different from the default value to avoid having collisions with the default library resources, since otherwise when those resources are used the default library ones may be loaded instead if both the embedded and original library are in the project, which could cause issues if the 2 libraries are of different versions with breaking changes between the 2 of them.
- **Template extension:** extension for the template file. It has to be different from the default value, to avoid collisions with the scripted importer of the 2 versions.
- **Collection extension:** extension for the template collection file. It has to be different from the default value, to avoid collisions with the scripted importer of the 2 versions.
- **Editor window menu path:** path to the editor windows in the menu, has to be different from the default one to avoid collisions with the base library, which would end up to being able to only have the options for 1 of the 2 libraries.
- **Create asset menu path:** path to the create asset menu in the menu, has to be different from the default one to avoid collisions with the base library, which would end up to being able to only have the options for 1 of the 2 libraries.

> [!IMPORTANT]
> You should keep an eye to what public shaders using the system use for these options, since you also have to not collide with them.

After setting all those options for the first time, it's a good idea to save them to a file, so that the next times you have to update the embedded library you can just load the options from the file.

Now it's time to embed the library by clicking `Embed`. It will prompt you to select a folder to where to put the library. The folder must be a folder called `Editor` since everything needs to be an editor script. Once done the editor will copy the library with the modifications declared and save it under a folder called `ModularShaderSystem` inside the selected editor folder.

From here you can use the embedded library directly to make your own shader. This has also the advantage that since you now have your own file extensions for templates and collections, they won't get mixed up with other's people modular shader templates.




