﻿// Decompiled by AS3 Sorcerer 1.99
// http://www.as3sorcerer.com/

//_zD_._N_N_

package _zD_ {
import _C__._cM_;

import _F_1.TitleView;
import _F_1.ServersView;

import _U_5._dd;

import _W_D_._0I_H_;

public class _N_N_ extends _cM_ {

    [Inject]
    public var view:ServersView;
    [Inject]
    public var _eJ_:_0I_H_;
    [Inject]
    public var _T__:_dd;

    override public function initialize():void {
        this.view._4s.add(this._G_P_);
        this.view.initialize(this._eJ_._T_1);
    }

    override public function destroy():void {
        this.view._4s.remove(this._G_P_);
    }

    private function _G_P_():void {
        this._T__.dispatch(new TitleView());
    }

}
}//package _zD_

