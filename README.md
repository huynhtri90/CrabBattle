<h2>Welcome to the Crab Battle</h2>

This codebase was initial submitted from Unity Forums. 

This project was created to help people get a Client / Server Game going using <a href="http://unity3d.com">Unity</a> and <a href="http://code.google.com/p/lidgren-network-gen3/">Lidgren</a>. Check back here or in <a href="http://forum.unity3d.com/threads/122560-Crab-Game-(Multiplayer-Client-Server-w-Lidgren-Source-Code)">forums</a> for updates. 

Let the games begin,<br>
Tom Acunzo - aka <i>Tomoprime</i>

<h3>Current Build v1.2</h3>
- Multiplayer 
Added support for changing player name while in game. GUIText will get updated on other clients.
Added Sequence Channel numbering grouped by message / packet types see GameServer.cs and NetworkManager.cs for additional notes.
- Fixed misc bugs. Clean up some warning messages.

<h3>Build v1.1</h3>

- Multiplayer 
Added late joiner support to access a game already in play.
Players can chat each other while in game.
Client side keepalives verify that idle connections are active one's (not corrupt). Server will drop them.
Threading has been improved on client side to help close unwanted connections.
Game difficulty and enemy health options are selectable while in game.
            
- Crab eyes now light up when play intro is disabled.
            
- Lots of a changes to code structure and tweaks to help make the code base more understandable.

- Settings have been moved to GameManager.cs and GameServer.cs

<b>Original Author:</b> Dan McNeill - aka <i>Doddler</i> Readme below...
<hr>
<pre>
About Source Code 

I'm always learning about coding practices and such, you'll notice the code is pretty messy. 
If you've seen I've done anything particularly erroneous, let me know! Everything that I made for the
game can be freely used for whatever, at your own risk etc. Stuff that isn't mine, well they have their
own liscences that you'll have to meet the requirements of.

Those components are as follows: 
Scripts and assets in the 3rd party folder weren't created by me and are bound by their own liscences.
This includes Lidgren.Network, Detonator, and iTween.  The project also uses Vectrosity to generate the
boundary circle. Since you actually have to buy vectrosity, it's not included in the project source. 
If you have vectrosity, you'll have to drop it into the 3rdParty\Vectrosity folder and uncomment the
line "#define Use_Vectrosity" at the start of crabmanager.cs.

The music and sounds I took from other sources, I would recommend not using them yourself!  
The music is "Aquaria Minibadass OC ReMix" by Daniel Baranowsky, and the sound effects are ripped from
the Japanese PC mecha/novel game Baldr Force EXE by Giga.  

Thanks to both of them, hopefully they won't get mad at me for using them!

Oh!  And CryptoHelper.cs was written by a friend, so please don't use or distribute that either.

I hope the code is helpful in someway and that you can learn something from it. :)

Dan "Doddler" McNeill (http://dodsrv.com/crabbattle/)
</pre>
