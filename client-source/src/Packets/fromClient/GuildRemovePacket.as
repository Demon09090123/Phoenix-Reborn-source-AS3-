﻿// Decompiled by AS3 Sorcerer 1.99
// http://www.as3sorcerer.com/

//_0A_g._tN_

package Packets.fromClient {
import flash.utils.IDataOutput;

public class GuildRemovePacket extends CliPacketError {

    public function GuildRemovePacket(_arg1:uint) {
        super(_arg1);
    }
    public var name_:String;

    override public function writeToOutput(_arg1:IDataOutput):void {
        _arg1.writeUTF(this.name_);
    }

    override public function toString():String {
        return (formatToString("GUILDREMOVE", "name_"));
    }

}
}//package _0A_g

