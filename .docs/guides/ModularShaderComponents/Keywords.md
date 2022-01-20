---
uid: str-Keywords
title: Keywords
---

# Keywords

Keywords are a specific piece of text placed all around templates, and are used as hook points to place other templates, function calls, variables, etc.

They should not be confused with [shader keywords](https://docs.unity3d.com/Manual/shader-keywords.html), as these keywords are only used by the modular shader system generation process, and generated shaders will not have any of these keywords in them.

A keyword is composed by `#K#` followed by a name like `EXAMPLE_KEYWORD`, with no other text in that line.

```

   // this is valid
   ...code...
   #K#NICE_KEYWORD 
   ...code...
   
   //this is not valid
   ...code...
   #K#NICE_KEYWORD  ...code...
   ...code...
   
   //this is also not valid
   ...code...
   ...code... #K#NICE_KEYWORD
   ...code...

```

There's also a variation with `#KI#` in place of `#K#`, in this case the keyword is considered `local` meaning that it is only usable from templates within the same module. Also functions don't have access to local keywords, they're for module templates only.

When referencing keywords inside shader module assets, you usually do not include the `#K#` prefix to identify the keyword, with the exception of the `Append After` value of a function definition, where the prefix is needed to identify the value as a keyword instead of a function call.