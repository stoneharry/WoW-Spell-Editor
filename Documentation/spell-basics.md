
### Spell Basics

In this document I will attempt to describe the basics of spell editing. If anything is not clear then please create a GitHub issue and I can update this document.

This documentation is for 3.3.5, but most of the concepts apply almost one-to-one with 2.4.3 and 1.12.

### The Basics

When you open the spell editor and select a spell, there are a lot of different tabs and fields available to view and edit.

 #### Select Spell
 
The Select Spell tab has all the text (strings) used by the spell. A single spell record can support every possible language the client supports, which is why there are many different tabs for each language. The spell tooltip and description supports spell formulas and tags that are derived by the client. For example, if you input the text `$d` then it will replace it with the duration of the spell.

A lot of the spell string tags are documented here: https://github.com/stoneharry/WoW-Spell-Editor/blob/master/Documentation/Vels%20Spell%20String%20Tag%20Documentation.txt

Some further documentation on spell formulas:

- `$17057d` - Display the duration of spell ID 17057.
- `$/10;17057s1` - Display the spell effect value 1 (base points + die sides) of spell ID 17057 divided by 10.
- `$/10;s1` - Display the spell effect value 1 (base points + die sides) of this spell divided by 10.
- `${1 + $s}` - Display all the spell effect values summed, plus 1.
- `$?(s108)[Hello world.][last for $d]` - If the player has spell ID 108, then display the text `Hello World.` else display the text `last for $d` where `$d` is the duration of the spell.

Not every tag and formula is supported by the spell editor.

#### Base

Most of the spell properties can be modified on the base tab. This is where you can set things like the cast time, range, power cost, and duration.
The drop-down menus do not allow you to input any value because the data loaded comes from other DBC files that the spell DBC references.

#### Effects

The effect targets allow for the target type of the spell.

Procs can be used to trigger events when a ‘proc’ happens. The charges is the number of times it can occur and the chance is a percentage. This data is usually used in parallel with the server spell proc data, which contains information such as ‘only on critical strikes’ etc.

A spell can have up to three spell effects. A spell effect is either something that happens when the spell hits the target, or an aura: `6 – APPLY_AURA`. An aura is a persistent effect on the player, whereas an effect happens once. If the spell effect is an aura, then the `Apply Aura Name` field is used to determine what the aura does.

Base points and die sides are used to calculate the value the effect/aura uses. For example, if the spell effect is `2 – SCHOOL_DAMAGE` and base points has a value of 99 and die sides 1 then that would result in the spell doing 100 damage. If the base points is 50 and the die sides is 50, then the spell would do 50 to 100 damage.

`TARGET_A` is used to determine the main target of the spell. Sometimes `TARGET_B` is used when you want a target of a target, for example from the caster to all enemy units within 10 yards.

`Misc Value A` and `Misc Value B` are used as generic fields to supply values to effects and auras. It’s meaning changes depending on the effect/aura. To understand this it is best to look at the emulator to see how it uses the data, or look at existing spells with the same effect/aura to see what the values mean.

#### Items

The Items tab defines any item requirements to use the spell. You can define totem requirements, or reagent costs. You can also define requirements such as needing to have a melee weapon equipped.

#### Flags

The Flags tab is a very important area for defining behaviour for the spell.

Interrupt flags can be used to determine when a spell should be interrupted.

Aura interrupt flags can be used to determine when an aura should be cancelled.

Channel interrupt flags can be used to determine when a channelled spell should be interrupted.

The attributes tabs can be used to flag all sorts of behaviour. I would recommend inspecting some of the values in the first Attributes tab since these are the most commonly used flags. You can flag behaviour such as hiding the spell client side, or making it a debuff (negative).

The misc column can be used to define creature type requirements or stance requirements.

### Dummy / Script effects

To do.

### Server side spells

Some spells exist only server side. This is because the client never needs to know about them. An example might be an `Infernal` spell, that launches an infernal at a position, spawning a creature when it hits the target. The missile visual and impact is client side, but it triggers a spell that exists server side and only spawns the infernal.

The server side spells are not supported by the spell editor and have to be manually edited in the emulator database.
