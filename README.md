WoW Spell Editor
===================

This is a spell editor designed to be used for WoW versions 3.3.5, 2.4.3, or 1.12.1. It also supports importing and exporting of any patch version DBC to and from SQL with the use of text file bindings found in the `Bindings` directory.

![Spell Editor Image 3.3.5a](https://i.imgur.com/Vpv4WcO.png)

Downloads can be found in the releases section, please report any issues you find and I will look at them when I get a chance.

The program currently supports SQLite and MySQL/MariaDB connection types.

The editing process is to import your `spell.dbc` into SQL, and then you can edit the spells with the program or with queries. When you are ready to test the modifications you have made you can export it back into a new `spell.dbc` file.

Config changes require a program restart to take effect.

Enjoy.

## Brief Guide

The `spell.dbc` exists client side inside the MPQ files: `DBFilesClient/Spell.dbc`. You extract this file and use it server side too. The client uses it to display spell strings, and other client side requirements. The server side uses it to calculate damage, healing, spell effects, etc, and validating that the requests the clients are sending are valid.

This means that if you wish to modify spells it is usually necessary to update both the client and the server. You can deploy a custom MPQ and if neccessary a custom wow.exe binary. There are plenty of guides for this out there, e.g: http://www.modcraft.io/index.php?board=78.0 / https://wowdev.wiki/Main_Page

----

When you first open the spell editor program it will ask which database type you want to use. The databases for editing the spells have different advantages and disadvantages:

**A) MySQL:**

When you run a WoW server you usually have a MySQL database running that handles the emulator data. You should create a new database and use this when the spell editor asks which database to use. The spell editor program will create all the tables for you with the correct structure when you import DBC files.

A MySQL table can have multiple people editing the data simultaneously. Anyone working on it can export the data to a new Spell.dbc at any point. You can also have multiple spell databases by changing which one the program is configured to use. You can setup different MySQL accounts so that different users have different permissions and anyone accessing the spell database cannot access the emulator databases.

MySQL also has many IDE's (SQLyog, Navicat, HediSQL, etc) that will allow you to query and perform operations on the spell tables with ease. This allows bulk operations, such as finding all spells that cost mana and changing it to energy.

B) **SQLite:**

As the name implies, this is a lightweight version of SQL. It will save all the data to a single local flat file. This means the program has no dependency on having a MySQL server running but only a single person can be working on the data at any given time. It is a lot harder to query this data too.

----

Once you have selected and configured the database you want to use, you can then import a spell.dbc file into the database. On the header for the program you will see a button for 'Import/Export'. Click this and select `spell.dbc`. You can import and export any other DBC files you have bindings for, but they must be inside the `DBC` folder distributed with the program.

After the spell.dbc has been imported you can edit spells at your leisure. You only need to import the spell.dbc once, you do not need to repeat this task each run of the program.

Once you are ready to test your spell changes in game, you must export the data back into a `spell.dbc`. Use the 'Import/Export' button on the header bar and select the `spell.dbc` in the export panel. The new DBC files will be created in a 'Export' folder in the same folder as the spell editor program.

This new spell.dbc file will need to be updated both client side and server side, as explained at the start of this little guide.

I hope this helps.

----

## Spell Visual Editor

![Spell Visual Tab Image](https://i.imgur.com/DZDIcLY.png)

The spell visual tab will only be populated when you import the visual DBC files. These will be automatically selected when you open the import/export window.

When a spell has no visual selected (value of 0) then a spell visual kit from another existing spell visual can be copied and pasted into the spell with no visual, resulting in the program copying the selected kit to the spell, creating a new spell visual and linking everything up.

You can copy and paste kits and effects/attachments freely. Whenever you paste it creates a new copy of the item you are pasting rather than using the existing one. This prevents you accidentally modifying another spell.

This feature is in its very early stages. Some values from the visual kit data are not handled in the UI. Other features can be added like displaying other spells using the same kit/effect/attachment or generally being able to search all existing objects.

The full class model is not currently supported. The spell visual class model looks like this:
![Spell Visual Class Model](https://i.imgur.com/o7mPR9k.png)

## Spell Visual Map

I wrote a utility tool for creating a map of creatures that show the spell visuals for every spell in the game. This was useful when trying to find which spell visual I wanted to use when creating custom spells.

This is now built into the spell editor. It is on the Visual, Map Builder tab. It defaults to map 13 which you can reach by running the command: `.go xyz 0 0 -15 13`

You will probably need to update the template SQL statements to match your emulator structure.

The files are exported to a Export folder.

[![Spell Visual Map video](https://img.youtube.com/vi/lU4Nn_mRS9U/maxresdefault.jpg)](https://www.youtube.com/watch?v=lU4Nn_mRS9U)

## Headless Exporter

The `HeadlessExporter` code is included in this repository. A guide on how to make use of this and precompiled binaries are available on the [DBC Editing Workflow](https://github.com/stoneharry/DBC-Editing-Workflow) repository.
