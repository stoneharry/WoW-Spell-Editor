WoW Spell Editor (3.3.5a - 12340)
===================

This is a spell editor designed to be used for WoW version 3.3.5a (12340). It also supports importing and exporting of any patch version DBC to and from SQL with the use of text file bindings found in the `Bindings` directory.

![Spell Editor Image](https://i.imgur.com/j7f8Fhb.png)

You could update it the program to be used with other patch versions relatively easily, but there is no easy way for me to make it support many versions at once with the current implementation.

Downloads can be found in the releases section, please report any issues you find and I will look at them when I get a chance.

The program currently supports SQLite and MySQL connection types.

The editing process is to import your `spell.dbc` into MySQL or SQLite, and then you can edit the spells with the program or with queries. When you are ready to test the modifications you have made you can export it back into a new `spell.dbc` file.

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

## Spell Visual Map

I wrote a utility tool for creating a map of creatures that show the spell visuals for every spell in the game. This was useful when trying to find which spell visual I wanted to use when creating custom spells.

My generated files are distributed with the releases of this program in `\WoW Spell Editor\SpellVisualMapBuilder\Export`. The program is hardcoded to use those entries and map 13. You can see this in action in the following video:

[![Spell Visual Map video](https://img.youtube.com/vi/lU4Nn_mRS9U/maxresdefault.jpg)](https://www.youtube.com/watch?v=lU4Nn_mRS9U)
