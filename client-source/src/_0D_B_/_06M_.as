﻿// Decompiled by AS3 Sorcerer 1.99
// http://www.as3sorcerer.com/

//_0D_B_._06M_

package _0D_B_ {
import com.company.PhoenixRealmClient.map._X_l;
import com.company.PhoenixRealmClient.objects.Player;
import com.company.PhoenixRealmClient.parameters.Parameters;
import com.company.PhoenixRealmClient.ui.CharObjectHeader;
import com.company.PhoenixRealmClient.ui.TooltipDivider;

import flash.events.Event;
import flash.events.MouseEvent;

public class _06M_ extends Menu {

    public function _06M_(_arg1:_X_l, _arg2:Vector.<Player>) {
        var _local4:Player;
        var _local5:CharObjectHeader;
        this._o2 = new Vector.<CharObjectHeader>();
        super(Parameters._primaryColourDefault, 0xFFFFFF);
        this.map_ = _arg1;
        this._nC_ = _arg2.concat();
        var _local3:int = 4;
        if (this.map_.allowPlayerTeleport_) {
            this._X_n = new _K_h(this.map_.player_);
            this._X_n.x = 8;
            this._X_n.y = 8;
            this._X_n.addEventListener(MouseEvent.CLICK, this._L_H_);
            addChild(this._X_n);
            this._6t = new TooltipDivider((width - 24), Parameters._primaryColourDark);
            this._6t.x = 6;
            this._6t.y = 40;
            addChild(this._6t);
            _local3 = 52;
        }
        for each (_local4 in this._nC_) {
            _local5 = new CharObjectHeader(0xB3B3B3, true, _local4);
            _local5.x = 0;
            _local5.y = _local3;
            addChild(_local5);
            this._o2.push(_local5);
            _local3 = (_local3 + 32);
        }
    }
    public var map_:_X_l;
    public var _nC_:Vector.<Player>;
    public var _X_n:_0K_G_;
    public var _6t:TooltipDivider;
    private var _o2:Vector.<CharObjectHeader>;

    private function _L_H_(_arg1:Event):void {
        var _local4:Player;
        var _local2:Player = this.map_.player_;
        var _local3:Player;
        for each (_local4 in this._nC_) {
            if (_local2.IsPlayerTargetable(_local4)) {
                _local3 = _local4;
                break;
            }
        }
        if (_local3 != null) {
            _local2.teleportTo(_local3);
        } else {
            this.map_.gs_.textBox_.addText(Parameters.SendError, "No players are eligible for teleporting.");
        }
        remove();
    }

}
}//package _0D_B_

