using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;

namespace EpgTimer
{
    public class EpgServiceAllEventInfo //: EpgServiceEventInfo
    {
        public readonly EpgServiceInfo serviceInfo;
        public readonly List<EpgEventInfo> eventList;
        public readonly List<EpgEventInfo> eventArcList;
        public readonly List<EpgEventInfo> eventMergeList;
        public EpgServiceAllEventInfo(EpgServiceInfo serviceInfo, List<EpgEventInfo> eventList = null, List<EpgEventInfo> eventArcList = null)
        {
            this.serviceInfo = serviceInfo;
            this.eventList = eventList ?? new List<EpgEventInfo>();
            this.eventArcList = eventArcList ?? new List<EpgEventInfo>();

            //基本情報のEPGデータだけ未更新だったりするときがあるようなので一応重複確認する
            //重複排除は開始時刻のみチェックなので、幾つかに分割されている場合などは不十分になる
            //過去番組はアーカイブを優先する
            if (this.eventList.Count != 0 && this.eventArcList.Count != 0)
            {
                var timeSet = new HashSet<DateTime>(eventArcList.Select(data => data.PgStartTime));//無いはずだが時間未定(PgStartTime=DateTime.MinValue)は吸収
                var addList = eventList.Where(data => timeSet.Contains(data.PgStartTime) == false);//時間未定(PgStartTime=DateTime.MaxValue)は通過する。
                this.eventMergeList = eventArcList.Concat(addList).ToList();
            }
            else
            {
                this.eventMergeList = this.eventList.Count != 0 ? eventList : this.eventArcList;
            }
        }
    }

    class DBManager
    {
        private bool updateEpgData = true;
        private bool updateReserveInfo = true;
        private bool updateRecInfo = true;
        private bool updateAutoAddEpgInfo = true;
        private bool updateAutoAddManualInfo = true;
        private bool updatePlugInFile = true;
        private bool noAutoReloadEpg = false;
        private bool oneTimeReloadEpg = false;
        private bool updateEpgAutoAddAppend = true;
        private bool updateEpgAutoAddAppendReserveInfo = true;

        Dictionary<UInt32, RecFileInfoAppend> recFileAppendList = null;
        Dictionary<UInt32, ReserveDataAppend> reserveAppendList = null;
        Dictionary<UInt32, AutoAddDataAppend> manualAutoAddAppendList = null;
        Dictionary<UInt32, EpgAutoAddDataAppend> epgAutoAddAppendList = null;

        public Dictionary<UInt64, EpgServiceAllEventInfo> ServiceEventList { get; private set; }
        public Dictionary<UInt32, ReserveData> ReserveList { get; private set; }
        public Dictionary<UInt32, TunerReserveInfo> TunerReserveList { get; private set; }
        public Dictionary<UInt32, RecFileInfo> RecFileInfo { get; private set; }
        public List<String> RecNamePlugInList { get; private set; }
        public List<String> WritePlugInList { get; private set; }
        public Dictionary<UInt32, ManualAutoAddData> ManualAutoAddList { get; private set; }
        public Dictionary<UInt32, EpgAutoAddData> EpgAutoAddList { get; private set; }

        public AutoAddDataAppend GetManualAutoAddDataAppend(ManualAutoAddData master)
        {
            if (master == null) return null;

            //データ更新は必要になったときにまとめて行う
            //未使用か、ManualAutoAddData更新により古いデータ廃棄済みでデータが無い場合
            Dictionary<uint, AutoAddDataAppend> dict = manualAutoAddAppendList;
            if (dict == null)
            {
                dict = ManualAutoAddList.Values.ToDictionary(item => item.dataID, item => new AutoAddDataAppend(
                    ReserveList.Values.Where(info => info != null && info.IsEpgReserve == false && item.CheckPgHit(info)).ToList()));

                foreach (AutoAddDataAppend item in dict.Values) item.UpdateCounts();

                manualAutoAddAppendList = dict;
            }

            AutoAddDataAppend retv;
            if (dict.TryGetValue(master.dataID, out retv) == false)
            {
                retv = new AutoAddDataAppend();
            }
            return retv;
        }
        public EpgAutoAddDataAppend GetEpgAutoAddDataAppend(EpgAutoAddData master)
        {
            if (master == null) return null;

            //データ更新は必要になったときにまとめて行う
            var dict = epgAutoAddAppendList ?? new Dictionary<uint, EpgAutoAddDataAppend>();
            if (updateEpgAutoAddAppend == true)
            {
                List<EpgAutoAddData> srcList = EpgAutoAddList.Values.Where(data => dict.ContainsKey(data.dataID) == false).ToList();
                if (srcList.Count != 0)
                {
                    List<EpgSearchKeyInfo> keyList = srcList.RecSearchKeyList().Clone();
                    keyList.ForEach(key => key.keyDisabledFlag = 0); //無効解除

                    try
                    {
                        var list_list = new List<List<EpgEventInfo>>();
                        CommonManager.CreateSrvCtrl().SendSearchPgByKey(keyList, ref list_list);

                        //通常あり得ないが、コマンド成功にもかかわらず何か問題があった場合は飛ばす
                        if (srcList.Count == list_list.Count)
                        {
                            int i = 0;
                            foreach (EpgAutoAddData item in srcList)
                            {
                                dict.Add(item.dataID, new EpgAutoAddDataAppend(list_list[i++]));
                            }
                        }

                        epgAutoAddAppendList = dict;
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
                }

                updateEpgAutoAddAppend = false;
                updateEpgAutoAddAppendReserveInfo = true;//現時刻でのSearchList再作成も含む
            }

            //予約情報との突き合わせが古い場合
            if (updateEpgAutoAddAppendReserveInfo == true)
            {
                foreach (EpgAutoAddDataAppend item in dict.Values) item.UpdateCounts();
                updateEpgAutoAddAppendReserveInfo = false;
            }

            //SendSearchPgByKeyに失敗した場合などは引っかかる。
            EpgAutoAddDataAppend retv;
            if (dict.TryGetValue(master.dataID, out retv) == false)
            {
                retv = new EpgAutoAddDataAppend();
            }
            return retv;
        }
        public void ClearEpgAutoAddDataAppend(Dictionary<UInt32, EpgAutoAddData> oldList = null)
        {
            if (oldList == null) epgAutoAddAppendList = null;
            if (epgAutoAddAppendList == null) return;

            var xs = new System.Xml.Serialization.XmlSerializer(typeof(EpgSearchKeyInfo));
            var SearchKey2String = new Func<EpgAutoAddData, string>(epgdata =>
            {
                var ms = new MemoryStream();
                xs.Serialize(ms, epgdata.searchInfo);
                ms.Seek(0, SeekOrigin.Begin);
                return new StreamReader(ms).ReadToEnd();
            });

            //並べ替えによるID変更もあるので、内容ベースでAppendを再利用する。
            var dicOld = new Dictionary<string, EpgAutoAddDataAppend>();
            foreach (var info in oldList.Values)
            {
                EpgAutoAddDataAppend data;
                if (epgAutoAddAppendList.TryGetValue(info.dataID, out data) == true)
                {
                    string key = SearchKey2String(info);
                    if (dicOld.ContainsKey(key) == false)
                    {
                        dicOld.Add(key, data);
                    }
                }
            }
            var newAppend = new Dictionary<uint, EpgAutoAddDataAppend>();
            foreach (var info in EpgAutoAddList.Values)
            {
                string key = SearchKey2String(info);
                EpgAutoAddDataAppend append1;
                if (dicOld.TryGetValue(key, out append1) == true)
                {
                    //同一内容の検索が複数ある場合は同じデータを参照することになる。
                    //特に問題無いはずだが、マズいようなら何か対応する。
                    newAppend.Add(info.dataID, append1);
                }
            }
            epgAutoAddAppendList = newAppend;
        }

        public ReserveDataAppend GetReserveDataAppend(ReserveData master)
        {
            if (master == null) return null;

            Dictionary<uint, ReserveDataAppend> dict = reserveAppendList;
            if (dict == null)
            {
                dict = ReserveList.ToDictionary(data => data.Key, data => new ReserveDataAppend());
                reserveAppendList = dict;

                foreach (EpgAutoAddData item in EpgAutoAddList.Values)
                {
                    item.GetReserveList().ForEach(info => dict[info.ReserveID].EpgAutoList.Add(item));
                }

                foreach (ManualAutoAddData item in ManualAutoAddList.Values)
                {
                    item.GetReserveList().ForEach(info => dict[info.ReserveID].ManualAutoList.Add(item));
                }

                foreach (ReserveData item in ReserveList.Values.Where(data => data.IsEpgReserve == true))
                {
                    UInt64 key = item.Create64PgKey();
                    dict[item.ReserveID].MultipleEPGList.AddRange(ReserveList.Values.Where(data => data.Create64PgKey() == key && data != item));
                }

                foreach (ReserveDataAppend data in dict.Values)
                {
                    data.UpdateData();
                }
            }

            ReserveDataAppend retv;
            if (dict.TryGetValue(master.ReserveID, out retv) == false)
            {
                retv = new ReserveDataAppend();
            }
            return retv;
        }

        public RecFileInfoAppend GetRecFileAppend(RecFileInfo master, bool UpdateDB = false)
        {
            if (master == null) return null;

            if (recFileAppendList == null)
            {
                recFileAppendList = new Dictionary<uint, RecFileInfoAppend>();
            }

            RecFileInfoAppend retv = null;
            if (recFileAppendList.TryGetValue(master.ID, out retv) == false)
            {
                //UpdataDBのときは、取得出来なくても取得済み扱いにする。
                if (UpdateDB == true)
                {
                    ReadRecFileAppend(RecFileInfo.Values.Where(info => info.HasErrPackets == true));
                    recFileAppendList.TryGetValue(master.ID, out retv);
                }
                else
                {
                    var extraRecInfo = new RecFileInfo();
                    if (CommonManager.CreateSrvCtrl().SendGetRecInfo(master.ID, ref extraRecInfo) == ErrCode.CMD_SUCCESS)
                    {
                        retv = new RecFileInfoAppend(extraRecInfo);
                        recFileAppendList.Add(master.ID, retv);
                    }
                }
            }
            return retv ?? new RecFileInfoAppend(master);
        }
        public void ReadRecFileAppend(IEnumerable<RecFileInfo> rlist = null)
        {
            if (recFileAppendList == null)
            {
                recFileAppendList = new Dictionary<uint, RecFileInfoAppend>();
            }

            var list = (rlist ?? RecFileInfo.Values).Where(info => recFileAppendList.ContainsKey(info.ID) == false).ToList();
            if (list.Count == 0) return;

            try
            {
                var extraDatalist = new List<RecFileInfo>();
                if (CommonManager.CreateSrvCtrl().SendGetRecInfoList(list.Select(info => info.ID).ToList(), ref extraDatalist) == ErrCode.CMD_SUCCESS)
                {
                    extraDatalist.ForEach(item => recFileAppendList.Add(item.ID, new RecFileInfoAppend(item)));
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }

            //何か問題があった場合でも何度もSendGetRecInfoList()しないよう残りも全て登録してしまう。
            foreach (var item in list.Where(info => recFileAppendList.ContainsKey(info.ID) == false))
            {
                recFileAppendList.Add(item.ID, new RecFileInfoAppend(item, false));
            }
        }
        public void ClearRecFileAppend(bool connect = false)
        {
            if (recFileAppendList == null) return;

            if (Settings.Instance.RecInfoExtraDataCache == false ||
                connect == true && Settings.Instance.RecInfoExtraDataCacheKeepConnect == false)
            {
                recFileAppendList = null;
            }
            else if (connect == false && Settings.Instance.RecInfoExtraDataCacheOptimize == true)
            {
                //Appendリストにあるが、有効でないデータ(通信エラーなどで仮登録されたもの)を削除。
                var delList = recFileAppendList.Where(item => item.Value.IsValid == false).Select(item => item.Key).ToList();
                delList.ForEach(key => recFileAppendList.Remove(key));

                //現在の録画情報リストにないデータを削除。
                var delList2 = recFileAppendList.Keys.Where(key => RecFileInfo.ContainsKey(key) == false).ToList();
                delList2.ForEach(key => recFileAppendList.Remove(key));
            }
        }
        public void ResetRecFileErrInfo()
        {
            if (recFileAppendList == null) return;
            foreach (RecFileInfoAppend data in recFileAppendList.Values) data.SetUpdateNotify();
        }

        public DBManager()
        {
            ClearAllDB();
        }

        public void ClearAllDB()
        {
            ServiceEventList = new Dictionary<ulong, EpgServiceAllEventInfo>();
            ReserveList = new Dictionary<uint, ReserveData>();
            TunerReserveList = new Dictionary<uint, TunerReserveInfo>();
            RecFileInfo = new Dictionary<uint, RecFileInfo>();
            RecNamePlugInList = new List<string>();
            WritePlugInList = new List<string>();
            ManualAutoAddList = new Dictionary<uint, ManualAutoAddData>();
            EpgAutoAddList = new Dictionary<uint, EpgAutoAddData>();
            reserveAppendList = null;
            recFileAppendList = null;
            manualAutoAddAppendList = null;
            epgAutoAddAppendList = null;
        }

        /// <summary>EPGデータの自動取得を行うかどうか(NW用)</summary>
        public void SetNoAutoReloadEPG(bool noReload)
        {
            noAutoReloadEpg = noReload;
        }

        public void SetOneTimeReloadEpg()
        {
            oneTimeReloadEpg = true;
        }

        /// <summary>データの更新があったことを通知</summary>
        /// <param name="updateInfo">[IN]更新のあったデータのフラグ</param>
        public void SetUpdateNotify(UpdateNotifyItem updateInfo)
        {
            switch (updateInfo)
            {
                case UpdateNotifyItem.EpgData:
                    updateEpgData = true;
                    updateEpgAutoAddAppend = true;
                    epgAutoAddAppendList = null;//検索数が変わる。
                    reserveAppendList = null;
                    break;
                case UpdateNotifyItem.ReserveInfo:
                    updateReserveInfo = true;
                    manualAutoAddAppendList = null;
                    updateEpgAutoAddAppendReserveInfo = true;
                    reserveAppendList = null;
                    break;
                case UpdateNotifyItem.RecInfo:
                    updateRecInfo = true;
                    break;
                case UpdateNotifyItem.AutoAddEpgInfo:
                    updateAutoAddEpgInfo = true;
                    updateEpgAutoAddAppend = true;
                    reserveAppendList = null;
                    break;
                case UpdateNotifyItem.AutoAddManualInfo:
                    updateAutoAddManualInfo = true;
                    manualAutoAddAppendList = null;
                    reserveAppendList = null;
                    break;
                case UpdateNotifyItem.PlugInFile:
                    updatePlugInFile = true;
                    break;
            }
        }

        /// <summary>EPG更新フラグをオフにする(EpgTimerSrv直接起動時用)。</summary>
        public void ResetUpdateNotifyEpg() { updateEpgData = false; }

        /// <summary>EPGデータの更新があれば再読み込みする</summary>
        public ErrCode ReloadEpgData(bool immediately = false)
        {
            if (immediately == true) SetUpdateNotify(UpdateNotifyItem.EpgData);
            var ret = ErrCode.CMD_SUCCESS;
            try
            {
                if (updateEpgData == true && (noAutoReloadEpg == false || oneTimeReloadEpg == true))
                {
                    ServiceEventList = new Dictionary<ulong, EpgServiceAllEventInfo>();

                    var list = new List<EpgServiceEventInfo>();
                    ret = CommonManager.CreateSrvCtrl().SendEnumPgAll(ref list);
                    if (ret != ErrCode.CMD_SUCCESS) return ret;

                    var list2 = new List<EpgServiceEventInfo>();
                    if (Settings.Instance.EpgLoadArcInfo == true)
                    {
                        CommonManager.CreateSrvCtrl().SendEnumPgArcAll(ref list2);
                    }
                    foreach (EpgServiceEventInfo info in list)
                    {
                        UInt64 id = info.serviceInfo.Create64Key();
                        //対応する過去番組情報があれば付加する
                        int i = list2.FindIndex(info2 => id == info2.serviceInfo.Create64Key());
                        ServiceEventList.Add(id, new EpgServiceAllEventInfo(info.serviceInfo, info.eventList, i < 0 ? new List<EpgEventInfo>() : list2[i].eventList));
                    }
                    //過去番組情報が残っていればサービスリストに加える
                    foreach (EpgServiceEventInfo info in list2)
                    {
                        UInt64 id = info.serviceInfo.Create64Key();
                        if (ServiceEventList.ContainsKey(id) == false)
                        {
                            ServiceEventList.Add(id, new EpgServiceAllEventInfo(info.serviceInfo, new List<EpgEventInfo>(), info.eventList));
                        }
                    }

                    //リモコンIDの登録
                    ChSet5.SetRemoconID(ServiceEventList);

                    updateEpgData = false;
                    oneTimeReloadEpg = false;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            return ret;
        }

        /// <summary>予約情報の更新があれば再読み込みする</summary>
        public ErrCode ReloadReserveInfo(bool immediately = false)
        {
            if (immediately == true) SetUpdateNotify(UpdateNotifyItem.ReserveInfo);
            var ret = ErrCode.CMD_SUCCESS;
            try
            {
                if (updateReserveInfo == true)
                {
                    ReserveList = new Dictionary<uint, ReserveData>();
                    TunerReserveList = new Dictionary<uint, TunerReserveInfo>();
                    var list = new List<ReserveData>();
                    var list2 = new List<TunerReserveInfo>();

                    ret = CommonManager.CreateSrvCtrl().SendEnumReserve(ref list);
                    if (ret != ErrCode.CMD_SUCCESS) return ret;

                    ret = CommonManager.CreateSrvCtrl().SendEnumTunerReserve(ref list2);
                    if (ret != ErrCode.CMD_SUCCESS) return ret;

                    list.ForEach(info => ReserveList.Add(info.ReserveID, info));
                    list2.ForEach(info => TunerReserveList.Add(info.tunerID, info));

                    updateReserveInfo = false;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            return ret;
        }

        /// <summary>録画済み情報の更新があれば再読み込みする</summary>
        public ErrCode ReloadRecFileInfo(bool immediately = false)
        {
            if (immediately == true) SetUpdateNotify(UpdateNotifyItem.RecInfo);
            var ret = ErrCode.CMD_SUCCESS;
            try
            {
                if (updateRecInfo == true)
                {
                    RecFileInfo = new Dictionary<uint, RecFileInfo>();
                    var list = new List<RecFileInfo>();

                    ret = CommonManager.CreateSrvCtrl().SendEnumRecInfoBasic(ref list);
                    if (ret != ErrCode.CMD_SUCCESS) return ret;

                    list.ForEach(info => RecFileInfo.Add(info.ID, info));

                    ClearRecFileAppend();
                    updateRecInfo = false;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            return ret;
        }

        /// <summary>PlugInFileの再読み込み指定があればする</summary>
        public ErrCode ReloadPlugInFile(bool immediately = false)
        {
            if (immediately == true) SetUpdateNotify(UpdateNotifyItem.PlugInFile);
            var ret = ErrCode.CMD_SUCCESS;
            try
            {
                if (updatePlugInFile == true)
                {
                    var recNameList = new List<string>();
                    var writeList = new List<string>();
                    RecNamePlugInList = recNameList;
                    WritePlugInList = writeList;

                    ret = CommonManager.CreateSrvCtrl().SendEnumPlugIn(1, ref recNameList);
                    if (ret != ErrCode.CMD_SUCCESS) return ret;

                    ret = CommonManager.CreateSrvCtrl().SendEnumPlugIn(2, ref writeList);
                    if (ret != ErrCode.CMD_SUCCESS) return ret;

                    updatePlugInFile = false;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            return ret;
        }

        /// <summary>EPG自動予約登録情報の更新があれば再読み込みする</summary>
        public ErrCode ReloadEpgAutoAddInfo(bool immediately = false)
        {
            if (immediately == true) SetUpdateNotify(UpdateNotifyItem.AutoAddEpgInfo);
            var ret = ErrCode.CMD_SUCCESS;
            try
            {
                if (updateAutoAddEpgInfo == true)
                {
                    Dictionary<uint, EpgAutoAddData> oldList = EpgAutoAddList;
                    EpgAutoAddList = new Dictionary<uint, EpgAutoAddData>();
                    var list = new List<EpgAutoAddData>();

                    ret = CommonManager.CreateSrvCtrl().SendEnumEpgAutoAdd(ref list);
                    if (ret != ErrCode.CMD_SUCCESS) return ret;

                    list.ForEach(info => EpgAutoAddList.Add(info.dataID, info));

                    ClearEpgAutoAddDataAppend(oldList);
                    updateAutoAddEpgInfo = false;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            return ret;
        }


        /// <summary>自動予約登録情報の更新があれば再読み込みする</summary>
        public ErrCode ReloadManualAutoAddInfo(bool immediately = false)
        {
            if (immediately == true) SetUpdateNotify(UpdateNotifyItem.AutoAddManualInfo);
            var ret = ErrCode.CMD_SUCCESS;
            try
            {
                if (updateAutoAddManualInfo == true)
                {
                    ManualAutoAddList = new Dictionary<uint, ManualAutoAddData>();
                    var list = new List<ManualAutoAddData>();

                    ret = CommonManager.CreateSrvCtrl().SendEnumManualAdd(ref list);
                    if (ret != ErrCode.CMD_SUCCESS) return ret;

                    list.ForEach(info => ManualAutoAddList.Add(info.dataID, info));

                    updateAutoAddManualInfo = false;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            return ret;
        }

    }
}
