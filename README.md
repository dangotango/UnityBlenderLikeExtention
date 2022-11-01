# UnityBlenderLikeExtention

Blenderのようにショートカットキーで操作するためのエディタ拡張<br>
ショートカットの割り当てはBlenderLikeExtentionsShortCutProfile.jsonをインポートしてください<br>
overlay等を使用しているので古いverのエディタでは動かないと思います。<br>

できること<br>

テンキーでシーンビューカメラ操作<br>
正面,裏　1,shift+1<br>
左、右　3,shift+3<br>
上、下　7,shift+7<br>
透視投影、並行投影トグル　5 <br>
カメラ左右15度回転　4,6<br>
カメラ上下15度　2,8 <br>
ズームインアウト　＋、－<br>
焦点を選択中のオブジェクトに合わせる　.<br>
カメラのパン移動　shift+中ボタンのドラッグ<br>
カメラの回転　中ボタンのドラッグ<br>
ズームインアウト　マウススクロール<br>

トランスフォームの編集<br>
オブジェクトを選択した後に<br>
移動　G　<br>
回転　R<br>
拡縮　S　（ローカル軸のみ）<br>
軸を固定して移動、回転、拡縮　（G or R or S）>> (x or y or z or shift+x or shift+y or shift+z)<br>
数値を指定して移動、回転、拡縮　(G or R or S) >> 数字入力<br>
スナップ　ctrlを押しながら　<br>
微小　shiftを押しながら　<br>

3Dカーソル<br>
3Dカーソルを原点（0,0,0）に移動　shift + c <br>
3Dカーソルを移動　shift+右マウスボタン<br>

オーバーレイ(挙動の設定)<br>
軸(グローバル・ローカル)の切り替え　<br>
ピボット(eachOrigin,3dCursor,center,activeObject)の切り替え　<br>
スナップの設定(グリッド移動、グリッド回転、グリッド拡縮)<br>
3Dカーソルにオブジェクトをスナップさせるボタン<br>
オブジェクトに3Dカーソルをスナップさせるボタン<br>
