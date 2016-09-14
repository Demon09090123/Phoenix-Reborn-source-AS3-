﻿// Decompiled by AS3 Sorcerer 1.99
// http://www.as3sorcerer.com/

//_011._S_M_

package Packets.fromServer {

import com.company.PhoenixRealmClient.net.messages.data.InTradeItem;
import com.company.PhoenixRealmClient.util._wW_;

import flash.utils.IDataInput;

public class TradeStartPacket extends SvrPacketError {

    public function TradeStartPacket(_arg1:uint) {
        this.myItems_ = new Vector.<InTradeItem>();
        this.yourItems_ = new Vector.<InTradeItem>();
        super(_arg1);
    }
    public var myItems_:Vector.<InTradeItem>;
    public var yourName_:String;
    public var yourItems_:Vector.<InTradeItem>;

    override public function parseFromInput(_arg1:IDataInput):void {
        var _local2:int;
        var _local3:int = _arg1.readShort();
        _local2 = _local3;
        while (_local2 < this.myItems_.length) {
            _wW_._ay(this.myItems_[_local2]);
            _local2++;
        }
        this.myItems_.length = Math.min(_local3, this.myItems_.length);
        while (this.myItems_.length < _local3) {
            this.myItems_.push((_wW_._B_1(InTradeItem) as InTradeItem));
        }
        _local2 = 0;
        while (_local2 < _local3) {
            this.myItems_[_local2].parseFromInput(_arg1);
            _local2++;
        }
        this.yourName_ = _arg1.readUTF();
        _local3 = _arg1.readShort();
        _local2 = _local3;
        while (_local2 < this.yourItems_.length) {
            _wW_._ay(this.yourItems_[_local2]);
            _local2++;
        }
        this.yourItems_.length = Math.min(_local3, this.yourItems_.length);
        while (this.yourItems_.length < _local3) {
            this.yourItems_.push((_wW_._B_1(InTradeItem) as InTradeItem));
        }
        _local2 = 0;
        while (_local2 < _local3) {
            this.yourItems_[_local2].parseFromInput(_arg1);
            _local2++;
        }
    }

    override public function toString():String {
        return (formatToString("TRADESTART", "myItems_", "yourName_", "yourItems_"));
    }

}
}//package _011

