Decompiler for the UnrealEngine 3 version of UnrealScript. For now, tested only with Unreal Tournament 3, but may be compatible with other games as well (Gears of War, Mass Effect, Mirror's Edge etc.)

Binaries are not yet available - if you want to use it, you'll need to compile the source code (written in C# 3.0).

## Previous Work

The following resources were an immense help for reverse-engineering the format of Unreal Engine packages and UnrealScript bytecode:

  * [UT Packages](http://sourceforge.net/projects/utpackages/) contains a decompiler for the previous versions of UnrealScript (UnrealEngine? v2 and v2.5). While there is a significant number of new features in UnrealScript 3, the general bytecode structure and the majority of bytecodes are the same.
  * [UnrealScript Reference](http://udn.epicgames.com/Three/UnrealScriptReference.html) is a user-level description of the language.
  * [UT3 Mod Home](http://udn.epicgames.com/Three/UT3ModHome.html) contains the original UnrealScript source code for Unreal Tournament 3 (the same code can also be extracted from the game packages, but still it's useful to have a downloadable reference version).
