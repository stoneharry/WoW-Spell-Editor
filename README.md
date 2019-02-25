WoW Spell Editor (3.3.5a - 12340)
===================

This is a spell editor designed to be used for WoW version 3.3.5a (12340). It also supports importing/exporting of any patch version DBC <-> SQLite/MySQL with the use of text file bindings found in the `Bindings` directory.

You could update it to be used with other versions relatively easily, but there is no easy way for me to make it support many versions at once with the current implementation.

Downloads can be found in the releases section, please report any issues you find and I will look at them when I get a chance.

It currently supports SQLite and MySQL connection types.

The editing process is to import your spell.dbc into MySQL or SQLite, and then edit the spells from there. When you are ready to test the modifications you have made you can export it back into a new spell.dbc.

Enjoy.

## Brief Guide

The spell.dbc exists client side inside the MPQ files. DBFilesClient/Spell.dbc. You extract this file and use it server side too. The client uses it to display spell strings, and other client side requirements. The server side uses it to calculate damage, healing, spell effects, etc, and validating that the requests the clients are sending are valid.

This means that if you wish to modify spells it is usually necessary to update both the client and the server. You can deploy a custom MPQ and if neccessary a custom wow.exe binary. There are plenty of guides for this out there, e.g: http://www.modcraft.io/index.php?board=78.0

----

When you first open the spell editor program it will ask which database type you want to use. The databases for editing the spells have different advantages and disadvantages:

**A) MySQL:**

When you run a WoW server you usually have a MySQL database running that handles the emulator data. You can create a new database or use one of the existing ones created for WoW emulators. When the spell editor program asks which table to use, you should choose a name that does not already exist. The spell table distributed with TrinityCore world databases does not contain all the data contained within a spell record and is not compatible with the spell editor program. The spell editor program will create the table for you with the correct structure.

A MySQL table can have multiple people editing the data simultaneously. Anyone working on it can export the data to a new Spell.dbc at any point. You can also have multiple spell tables by changing the table name in the config. You can setup different MySQL accounts so that different users have different permissions and anyone accessing the spell database cannot access the emulator databases.

MySQL also has many IDE's (SQLyog, Navicat, HediSQL, etc) that will allow you to query and perform operations on the spell tables with ease. This allows bulk operations, such as finding all spells that cost mana and changing it to energy.

B) **SQLite:**

As the name implies, this is a lightweight version of SQL. It will save all the data to a single local flat file. This means the program has no dependency on having a MySQL server running but only a single person can be working on the data at any given time. It is a lot harder to query this data too.

----

Once you have selected and configured the database you want to use, you can then import a spell.dbc file into the database. On the header for the program you will see a button for 'Import/Export'. Click this and select `spell.dbc`. You can import and export any other DBC files you have bindings for, but they must be inside the `DBC` folder distributed with the program.

After the spell.dbc has been imported you can edit spells at your leisure. You only need to import the spell.dbc once, you do not need to repeat this task each run of the program.

Once you are ready to test your spell changes in game, you must export the data back into a `spell.dbc`. Use the 'Import/Export' button on the header bar and select the `spell.dbc` in the export panel. The new DBC files will be created in a 'Export' folder in the same folder as the spell editor program.

This new spell.dbc file will need to be updated both client side and server side, as explained at the start of this little guide.

I hope this helps.
