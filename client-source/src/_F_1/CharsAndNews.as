﻿// Decompiled by AS3 Sorcerer 1.99
// http://www.as3sorcerer.com/

//_F_1.CharsAndNews

package _F_1 {
import com.company.PhoenixRealmClient.appengine._0K_R_;
import com.company.PhoenixRealmClient.screens.charrects.CharacterRectList;

import flash.display.Graphics;
import flash.display.Shape;
import flash.display.Sprite;

public class CharsAndNews extends Sprite {

    public static const WIDTH:int = 760;
    public static const HEIGHT:int = 600;
    public static const charState = 1;
    public static const graveState = 2;

    public var list_:Sprite = new Sprite;

    public function CharsAndNews(_arg1:_0K_R_, _arg2:_05p, _arg3:int) {
        var _local3:Shape;
        var _local4:Graphics;
        super();
        switch (_arg3){
            case charState:
                this.list_ = new CharacterRectList(_arg1, _arg2);
                break;
            case graveState:
                this.list_ = new News(_arg1, _arg3);
                break;
            case 3:
                this.news_ = new News(_arg1, 1);
                addChild(this.news_);
                break;
            default:
                this.list_ = new Sprite();
        }
        addChild(this.list_);
        if (height > 600) {
            _local3 = new Shape();
            _local4 = _local3.graphics;
            _local4.beginFill(0);
            _local4.drawRect(0, 0, WIDTH, HEIGHT);
            _local4.endFill();
            addChild(_local3);
            mask = _local3;
        }
    }

    public function listScroll(_arg1:Number):void {
        this.list_.y = _arg1;
    }

    public var news_:News;

}
}//package _F_1

