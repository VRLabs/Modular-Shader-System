---
uid: adv-MigratingAssets
title: Migrating Assets
---

# Migrating Assets

If you embed the library for your specific project, you're quickly going to notice that all shaders, modules and templates you're made are still dependent on the original Modular Shader System installation, and not your newly embedded one.

You could theoretically just remake the modules and shaders, and changing the extensions of the templates and collections, but it is quite annoying to do that, especially considering that you're going to have to reassign all asset references.

For this reason there's a tool included  that lets you import and export assets from the library to a generic format and back, and can be found in `VRLabs > Modular Shader > Tools > Migrator`.

## Migrator

The migrator is a tool divided into 2 tabs: `Export` and `Import`:

![window](/images/docs/AdvancedTopics/2.png)
![window](/images/docs/AdvancedTopics/3.png)

The export section will let you select the assets you want to migrate, and by pressing the `Save` button. you can save them into a file.

On the other hand the import section will let you to read and import the mentioned file, restoring the previously selected modules in the same path under the library the migrator window belongs to.
You will always import all the assets contained into the migration files

> [!IMPORTANT]
> Import and export are always dependent on which library said window is from.
> `VRLabs > Modular Shader > Tools > Migrator` is where the default one is, meanwhile your embedded version depends on the embed settings.
> So to convert a modular shader from base library to your embedded library you have to open the default library migrator, export the assets you want, and then open the migrator of your embedded version and import the file from there.

> [!WARNING]
> Beware that if you have default texture overrides in some properties, you have to complete the migration process within the same editor session, otherwise the references to those textures will be lost. This is also a good reason to not use the migrator as a "quick share" tool





