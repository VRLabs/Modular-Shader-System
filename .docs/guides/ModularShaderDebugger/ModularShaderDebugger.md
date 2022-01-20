---
uid: dbg-ModularShaderDebugger
title: Modular Shader Debugger
---

# Modular Shader Debugger

The Modular Shader Debugger is a tool used to display various informations about a selected modular shader that is useful when you're in the process of creating a module for said shader.

![window](/images/docs/ModularShaderDebugger/1.png)

The window is composed by a top bar containing a field for the selected modular shader, and a button to reload the selected shader, in case the modular shader or one if its modules has been updated.

The rest of the window is composed by a tab row that by default contains 2 selectable tabs, the [Template Graph](xref:dbg-TemplateGraph) and the [Function Timeline](xref:dbg-FunctionTimeline).

The window can be extended by inheriting [IModularShaderDebuggerTab](xref:VRLabs.ModularShaderSystem.Debug.IModularShaderDebuggerTab). Custom tabs can be useful to show specific informations you may need to show in your custom modular shader project.

