using System;
using System.Collections.Generic;
using System.Linq;
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
                var timeSet = new HashSet<DateTime>(eventArcList.Where(data => data.StartTimeFlag != 0).Select(data => data.PgStartTime));//無いはずだが時間未定をリファレンスから除外
                var addList = eventList.Where(data => timeSet.Contains(data.PgStartTime) == false);//時間未定(PgStartTime=DateTime.MaxValue)は通過する。
                this.eventMergeList = eventArcList.Concat(addList).ToList();
            }
            else
            {
                this.eventMergeList = this.eventList.Count != 0 ? eventList : this.eventArcList;
            }
        }

        public static Dictionary<UInt64, EpgServiceAllEventInfo> CreateEpgServicDictionary(List<EpgServiceEventInfo> list, List<EpgServiceEventInfo> list2)
        {
            var serviceDic = new Dictionary<UInt64, EpgServiceAllEventInfo>();
            foreach (EpgServiceEventInfo info in list)
            {
                UInt64 id = info.serviceInfo.Create64Key();
                //対応する過去番組情報があれば付加する
                int i = list2.FindIndex(info2 => id == info2.serviceInfo.Create64Key());
                serviceDic[id] = new EpgServiceAllEventInfo(info.serviceInfo, info.eventList, i < 0 ? new List<EpgEventInfo>() : list2[i].eventList);
            }
            //過去番組情報が残っていればサービスリストに加える
            foreach (EpgServiceEventInfo info in list2)
            {
                UInt64 id = info.serviceInfo.Create64Key();
                if (serviceDic.ContainsKey(id) == false)
                {
                    serviceDic[id] = new EpgServiceAllEventInfo(info.serviceInfo, new List<EpgEventInfo>(), info.eventList);
                }
            }
            return serviceDic;
        }
    }

    class DBManager
    {
        public readonly Dictionary<UpdateNotifyItem, Action> DBChanged = new Dictionary<UpdateNotifyItem, Action>();
        private HashSet<UpdateNotifyItem> upDateNotify = new HashSet<UpdateNotifyItem>(Enum.GetValues(typeof(UpdateNotifyItem)).Cast<UpdateNotifyItem>());

        private bool updateEpgAutoAddAppend = true;
        private bool updateEpgAutoAddAppendReserveInfo = true;
        private bool updateReserveAppendEpgAuto = true;
        private bool updateReserveAppendManualAuto = true;

        Dictionary<UInt32, RecFileInfoAppend> recFileAppendList = null;
        Dictionary<UInt32, ReserveDataAppend> reserveAppendList = null;
        Dictionary<UInt32, EpgEventInfo> reserveEventList = null;
        Dictionary<UInt64, EpgEventInfo> reserveEventListCache = null;
        HashSet<UInt32> reserveMultiList = null;
        Dictionary<UInt32, AutoAddDataAppend> manualAutoAddAppendList = null;
        Dictionary<UInt32, EpgAutoAddDataAppend> epgAutoAddAppendList = null;

        public Dictionary<UInt64, EpgServiceAllEventInfo> ServiceEventList { get; private set; }
        public Dictionary<UInt32, ReserveData> ReserveList { get; private set; }
        public Dictionary<UInt32, TunerReserveInfo> TunerReserveList { get; private set; }
        //public RecSettingData DefaultRecSetting { get; private set; }
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
            if (manualAutoAddAppendList == null)
            {
                manualAutoAddAppendList = ManualAutoAddList.Values.ToDictionary(item => item.dataID, item => new AutoAddDataAppend(
                    ReserveList.Values.Where(info => info != null && info.IsEpgReserve == false && item.CheckPgHit(info)).ToList()));

                foreach (AutoAddDataAppend item in manualAutoAddAppendList.Values) item.UpdateCounts();
            }

            AutoAddDataAppend retv;
            manualAutoAddAppendList.TryGetValue(master.dataID, out retv);
            return retv ?? new AutoAddDataAppend();
        }
        public EpgAutoAddDataAppend GetEpgAutoAddDataAppend(EpgAutoAddData master)
        {
            if (master == null) return null;

            //データ更新は必要になったときにまとめて行う
            var dict = epgAutoAddAppendList ?? new Dictionary<uint, EpgAutoAddDataAppend>();
            if (updateEpgAutoAddAppend == true)
            {
                List<EpgAutoAddData> srcList = EpgAutoAddList.Values.Where(data => dict.ContainsKey(data.dataID) == false).ToList();
                if (srcList.Count != 0 && Settings.Instance.NoEpgAutoAddAppend == false)
                {
                    List<EpgSearchKeyInfo> keyList = srcList.RecSearchKeyList().DeepClone();
                    keyList.ForEach(key => key.keyDisabledFlag = 0); //無効解除

                    var list_list = new List<List<EpgEventInfo>>();
                    try { CommonManager.CreateSrvCtrl().SendSearchPgByKey(keyList, ref list_list); }
                    catch { }

                    //通常あり得ないが、コマンド成功にもかかわらず何か問題があった場合は飛ばす
                    if (srcList.Count == list_list.Count)
                    {
                        int i = 0;
                        foreach (EpgAutoAddData item in srcList)
                        {
                            dict[item.dataID] = new EpgAutoAddDataAppend(list_list[i++]);
                        }
                    }
                }

                epgAutoAddAppendList = dict;
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
            dict.TryGetValue(master.dataID, out retv);
            return retv ?? new EpgAutoAddDataAppend();
        }
        public void ClearEpgAutoAddDataAppend(Dictionary<UInt32, EpgAutoAddData> oldList = null)
        {
            if (oldList == null || Settings.Instance.NoEpgAutoAddAppend == true) epgAutoAddAppendList = null;
            if (epgAutoAddAppendList == null) return;

            var xs = new System.Xml.Serialization.XmlSerializer(typeof(EpgSearchKeyInfo));
            var SearchKey2String = new Func<EpgAutoAddData, string>(epgdata =>
            {
                var sr = new StringWriter();
                xs.Serialize(sr, epgdata.searchInfo);
                return sr.ToString();
            });

            //並べ替えによるID変更もあるので、内容ベースでAppendを再利用する。
            var dicOld = new Dictionary<string, EpgAutoAddDataAppend>();
            foreach (var info in oldList.Values)
            {
                EpgAutoAddDataAppend data;
                if (epgAutoAddAppendList.TryGetValue(info.dataID, out data) == true)
                {
                    dicOld[SearchKey2String(info)] = data;
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
                    newAppend[info.dataID] = append1;
                }
            }
            epgAutoAddAppendList = newAppend;
        }

        public ReserveDataAppend GetReserveDataAppend(ReserveData master)
        {
            if (master == null) return null;

            if (reserveAppendList == null)
            {
                reserveAppendList = ReserveList.ToDictionary(data => data.Key, data => new ReserveDataAppend());
                updateReserveAppendEpgAuto = true;
                updateReserveAppendManualAuto = true;
            }
            //キーワード予約が更新された場合
            if (updateReserveAppendEpgAuto == true)
            {
                foreach (ReserveDataAppend data in reserveAppendList.Values) data.EpgAutoList.Clear();
                foreach (EpgAutoAddData item in EpgAutoAddList.Values)
                {
                    item.GetReserveList().ForEach(info => reserveAppendList[info.ReserveID].EpgAutoList.Add(item));
                }
            }
            //プログラム予約が更新された場合
            if (updateReserveAppendManualAuto == true)
            {
                foreach (ReserveDataAppend data in reserveAppendList.Values) data.ManualAutoList.Clear();
                foreach (ManualAutoAddData item in ManualAutoAddList.Values)
                {
                    item.GetReserveList().ForEach(info => reserveAppendList[info.ReserveID].ManualAutoList.Add(item));
                }
            }
            //その他データの再構築
            if (updateReserveAppendEpgAuto == true || updateReserveAppendManualAuto == true)
            {
                foreach (ReserveDataAppend data in reserveAppendList.Values) data.UpdateData();
                updateReserveAppendEpgAuto = false;
                updateReserveAppendManualAuto = false;
            }

            ReserveDataAppend retv;
            reserveAppendList.TryGetValue(master.ReserveID, out retv);
            return retv ?? new ReserveDataAppend();
        }

        public bool IsReserveMulti(ReserveData master)
        {
            if (master == null) return false;

            if (reserveMultiList == null)
            {
                reserveMultiList = new HashSet<uint>(ReserveList.Values.Where(data => data.IsEpgReserve == true)
                                    .GroupBy(data => data.Create64PgKey(), data => data.ReserveID)
                                    .Where(gr => gr.Count() > 1).SelectMany(gr => gr));
            }

            return reserveMultiList.Contains(master.ReserveID);
        }

        public EpgEventInfo GetReserveEventList(ReserveData master)
        {
            if (master == null) return null;

            if (reserveEventList == null)
            {
                if (ServiceEventList.Count != 0 && IsNotifyRegistered(UpdateNotifyItem.EpgData) == false || Settings.Instance.NoReserveEventList == true)
                {
                    reserveEventList = ReserveList.Values.ToDictionary(rs => rs.ReserveID,
                        rs => rs.IsEpgReserve == true ? MenuUtil.SearchEventInfo(rs.Create64PgKey()) : rs.SearchEventInfoLikeThat());
                }
                else
                {
                    reserveEventList = new Dictionary<uint, EpgEventInfo>();
                    reserveEventListCache = reserveEventListCache ?? new Dictionary<ulong, EpgEventInfo>();

                    //要求はしないが、有効なデータが既に存在していればキーワード予約の追加データを参照する。
                    bool useAppend = epgAutoAddAppendList != null && updateEpgAutoAddAppend == false 
                        && updateEpgAutoAddAppendReserveInfo == false;

                    var trgList = new List<ReserveData>();
                    foreach (ReserveData data in ReserveList.Values)
                    {
                        EpgEventInfo info = null;
                        ulong key = data.Create64PgKey();
                        if (useAppend == true)
                        {
                            List<EpgAutoAddData> epgAutoList = data.GetEpgAutoAddList();
                            if (epgAutoList.Count != 0)
                            {
                                SearchItem item = epgAutoList[0].GetSearchList()
                                    .Find(sI => sI.IsReserved == true && sI.ReserveInfo.ReserveID == data.ReserveID);
                                if (item != null)
                                {
                                    info = item.EventInfo;
                                    reserveEventListCache[key] = info;
                                }
                            }
                        }
                        if (info == null) reserveEventListCache.TryGetValue(key, out info);
                        if (info != null)
                        {
                            reserveEventList[data.ReserveID] = info;
                        }
                        else
                        {
                            trgList.Add(data);
                        }
                    }

                    var pgIDset = trgList.ToLookup(data => data.Create64PgKey(), data => data.ReserveID);
                    if (pgIDset.Any() == true)
                    {
                        var keys = pgIDset.Select(lu => lu.Key).ToList();
                        var list = new List<EpgEventInfo>();
                        try { CommonManager.CreateSrvCtrl().SendGetPgInfoList(keys, ref list); }
                        catch { }

                        foreach (EpgEventInfo info in list)
                        {
                            ulong key = info.Create64PgKey();
                            if (pgIDset.Contains(key) == true)
                            {
                                foreach (uint rID in pgIDset[key])
                                {
                                    reserveEventList[rID] = info;
                                }
                                reserveEventListCache[key] = info;
                            }
                        }
                    }
                }
            }

            EpgEventInfo retv;
            reserveEventList.TryGetValue(master.ReserveID, out retv);
            return retv;
        }

        public RecFileInfoAppend GetRecFileAppend(RecFileInfo master, bool UpdateDB = false)
        {
            if (master == null) return null;

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
                    try
                    {
                        var extraRecInfo = new RecFileInfo();
                        if (CommonManager.CreateSrvCtrl().SendGetRecInfo(master.ID, ref extraRecInfo) == ErrCode.CMD_SUCCESS)
                        {
                            retv = new RecFileInfoAppend(extraRecInfo);
                            recFileAppendList[master.ID] = retv;
                        }
                    }
                    catch { }
                }
            }
            return retv ?? new RecFileInfoAppend(master);
        }
        public void ReadRecFileAppend(IEnumerable<RecFileInfo> rlist = null)
        {
            var list = (rlist ?? RecFileInfo.Values).Where(info => recFileAppendList.ContainsKey(info.ID) == false).ToList();
            if (list.Count == 0) return;

            try
            {
                var extraDatalist = new List<RecFileInfo>();
                if (CommonManager.CreateSrvCtrl().SendGetRecInfoList(list.Select(info => info.ID).ToList(), ref extraDatalist) == ErrCode.CMD_SUCCESS)
                {
                    extraDatalist.ForEach(item => recFileAppendList[item.ID] = new RecFileInfoAppend(item));
                }
            }
            catch { }

            //何か問題があった場合でも何度もSendGetRecInfoList()しないよう残りも全て登録してしまう。
            foreach (var item in list.Where(info => recFileAppendList.ContainsKey(info.ID) == false))
            {
                recFileAppendList[item.ID] = new RecFileInfoAppend(item, false);
            }
        }
        public void ClearRecFileAppend()
        {
            recFileAppendList = new Dictionary<uint, RecFileInfoAppend>();
        }
        public void ResetRecFileErrInfo()
        {
            foreach (RecFileInfoAppend data in recFileAppendList.Values) data.SetUpdateNotify();
        }

        public DBManager()
        {
            ClearRecFileAppend();
            ServiceEventList = new Dictionary<ulong, EpgServiceAllEventInfo>();
            ReserveList = new Dictionary<uint, ReserveData>();
            TunerReserveList = new Dictionary<uint, TunerReserveInfo>();
            //DefaultRecSetting = null;
            RecFileInfo = new Dictionary<uint, RecFileInfo>();
            RecNamePlugInList = new List<string>();
            WritePlugInList = new List<string>();
            ManualAutoAddList = new Dictionary<uint, ManualAutoAddData>();
            EpgAutoAddList = new Dictionary<uint, EpgAutoAddData>();
        }

        /// <summary>データの更新があったことを通知</summary>
        /// <param name="updateInfo">[IN]更新のあったデータのフラグ</param>
        public void SetUpdateNotify(UpdateNotifyItem notify)
        {
            upDateNotify.Add(notify);
            if (notify == UpdateNotifyItem.EpgData)
            {
                updateEpgAutoAddAppend = true;
                epgAutoAddAppendList = null;//検索数が変わる。
                reserveEventList = null;
                reserveEventListCache = null;
            }
        }
        public bool IsNotifyRegistered(UpdateNotifyItem notify)
        {
            return upDateNotify.Contains(notify);
        }
        public void ResetUpdateNotify(UpdateNotifyItem notify)
        {
            ResetNotifyWork(notify, true);
        }
        private void ResetNotifyWork(UpdateNotifyItem notify, bool resetOnly = false)
        {
            if (resetOnly == false && upDateNotify.Contains(notify) == true)
            {
                Action postFunc;
                if (DBChanged.TryGetValue(notify, out postFunc) == true && postFunc != null)
                {
                    postFunc();
                }
            }
            upDateNotify.Remove(notify);
        }
        private ErrCode ReloadWork(UpdateNotifyItem notify, bool immediately, bool noRaiseChanged, Func<ErrCode, ErrCode> work)
        {
            if (immediately == true) SetUpdateNotify(notify);
            var ret = ErrCode.CMD_SUCCESS;
            if (IsNotifyRegistered(notify) == true)
            {
                ret = work(ret);
                if (ret == ErrCode.CMD_SUCCESS) ResetNotifyWork(notify, noRaiseChanged);
            }
            return ret;
        }

        /// <summary>EPGデータの更新があれば再読み込みする</summary>
        public ErrCode ReloadEpgData(bool immediately = false, bool noRaiseChanged = false)
        {
            return ReloadWork(UpdateNotifyItem.EpgData, immediately, noRaiseChanged, ret =>
            {
                ServiceEventList = new Dictionary<ulong, EpgServiceAllEventInfo>();

                var list = new List<EpgServiceEventInfo>();
                try { ret = CommonManager.CreateSrvCtrl().SendEnumPgAll(ref list); } catch { ret = ErrCode.CMD_ERR; }
                if (ret != ErrCode.CMD_SUCCESS) return ret;

                var list2 = new List<EpgServiceEventInfo>();
                if (Settings.Instance.EpgNoDisplayOldDays > 0)
                {
                    try { CommonManager.CreateSrvCtrl().SendEnumPgArc(new List<long> { 0xFFFFFFFFFFFF, 0xFFFFFFFFFFFF, DateTime.UtcNow.AddHours(9).AddDays(-1 - Settings.Instance.EpgNoDisplayOldDays).Date.ToFileTime(), long.MaxValue }, ref list2); } catch { }
                }

                //複合リストの作成
                ServiceEventList = EpgServiceAllEventInfo.CreateEpgServicDictionary(list, list2);

                //リモコンIDの登録
                ChSet5.SetRemoconID(ServiceEventList);

                reserveEventList = null;
                reserveEventListCache = null;
                return ret;
            });
        }

        /// <summary>予約情報の更新があれば再読み込みする</summary>
        public ErrCode ReloadReserveInfo(bool immediately = false, bool noRaiseChanged = false)
        {
            return ReloadWork(UpdateNotifyItem.ReserveInfo, immediately, noRaiseChanged, ret =>
            {
                ReserveList = new Dictionary<uint, ReserveData>();
                TunerReserveList = new Dictionary<uint, TunerReserveInfo>();
                var list = new List<ReserveData>();
                var list2 = new List<TunerReserveInfo>();
                //var resinfo = new ReserveData();

                try { ret = CommonManager.CreateSrvCtrl().SendEnumReserve(ref list); } catch { ret = ErrCode.CMD_ERR; }
                if (ret != ErrCode.CMD_SUCCESS) return ret;

                try { ret = CommonManager.CreateSrvCtrl().SendEnumTunerReserve(ref list2); } catch { ret = ErrCode.CMD_ERR; }
                if (ret != ErrCode.CMD_SUCCESS) return ret;

                //try { ret = CommonManager.CreateSrvCtrl().SendGetReserve(0x7FFFFFFF, ref resinfo); } catch { ret = ErrCode.CMD_ERR; }
                //if (ret != ErrCode.CMD_SUCCESS) return ret;

                list.ForEach(info => ReserveList[info.ReserveID] = info);
                list2.ForEach(info => TunerReserveList[info.tunerID] = info);
                //DefaultRecSetting = resinfo.RecSetting;

                reserveAppendList = null;
                reserveMultiList = null;
                reserveEventList = null;
                updateEpgAutoAddAppendReserveInfo = true;
                manualAutoAddAppendList = null;
                ResetNotifyWork(UpdateNotifyItem.ReserveName, true);
                return ret;
            });
        }

        /// <summary>
        /// 予約情報の録画予定ファイル名のみ再読み込みする
        /// </summary>
        /// <returns></returns>
        public ErrCode ReloadReserveRecFileNameList(bool immediately = false, bool noRaiseChanged = false)
        {
            return ReloadWork(UpdateNotifyItem.ReserveName, immediately, noRaiseChanged, ret =>
            {
                bool changed = false;
                if (ReserveList.Count > 0)
                {
                    var list = new List<ReserveData>();
                    try { ret = CommonManager.CreateSrvCtrl().SendEnumReserve(ref list); } catch { ret = ErrCode.CMD_ERR; }
                    if (ret != ErrCode.CMD_SUCCESS) return ret;

                    foreach (ReserveData info in list)
                    {
                        if (ReserveList.ContainsKey(info.ReserveID))
                        {
                            if (ReserveList[info.ReserveID].RecFileNameList.Count != info.RecFileNameList.Count)
                            {
                                ReserveList[info.ReserveID].RecFileNameList = info.RecFileNameList;
                                changed = true;
                            }
                            else
                            {
                                for (int i = 0; i < info.RecFileNameList.Count; i++)
                                {
                                    if (ReserveList[info.ReserveID].RecFileNameList[i] != info.RecFileNameList[i])
                                    {
                                        ReserveList[info.ReserveID].RecFileNameList = info.RecFileNameList;
                                        changed = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                ResetNotifyWork(UpdateNotifyItem.ReserveName, !changed || noRaiseChanged);
                return ret;
            });
        }

        /// <summary>録画済み情報の更新があれば再読み込みする</summary>
        public ErrCode ReloadRecFileInfo(bool immediately = false, bool noRaiseChanged = false)
        {
            return ReloadWork(UpdateNotifyItem.RecInfo, immediately, noRaiseChanged, ret =>
            {
                RecFileInfo = new Dictionary<uint, RecFileInfo>();
                var list = new List<RecFileInfo>();

                try { ret = CommonManager.CreateSrvCtrl().SendEnumRecInfoBasic(ref list); } catch { ret = ErrCode.CMD_ERR; }
                if (ret != ErrCode.CMD_SUCCESS) return ret;

                list.ForEach(info => RecFileInfo[info.ID] = info);

                //無効データ(通信エラーなどで仮登録されたもの)と録画結果一覧に無いデータを削除して再構築。
                recFileAppendList = recFileAppendList.Where(item => item.Value.IsValid == true && RecFileInfo.ContainsKey(item.Key) == true).ToDictionary(item => item.Key, item => item.Value);

                return ret;
            });
        }

        /// <summary>PlugInFileの再読み込み指定があればする</summary>
        public ErrCode ReloadPlugInFile(bool immediately = false, bool noRaiseChanged = false)
        {
            return ReloadWork(UpdateNotifyItem.PlugInFile, immediately, noRaiseChanged, ret =>
            {
                var recNameList = new List<string>();
                var writeList = new List<string>();
                RecNamePlugInList = recNameList;
                WritePlugInList = writeList;

                try { ret = CommonManager.CreateSrvCtrl().SendEnumPlugIn(1, ref recNameList); } catch { ret = ErrCode.CMD_ERR; }
                if (ret != ErrCode.CMD_SUCCESS) return ret;

                try { ret = CommonManager.CreateSrvCtrl().SendEnumPlugIn(2, ref writeList); } catch { ret = ErrCode.CMD_ERR; }
                return ret;
            });
        }

        /// <summary>EPG自動予約登録情報の更新があれば再読み込みする</summary>
        public ErrCode ReloadEpgAutoAddInfo(bool immediately = false, bool noRaiseChanged = false)
        {
            return ReloadWork(UpdateNotifyItem.AutoAddEpgInfo, immediately, noRaiseChanged, ret =>
            {
                Dictionary<uint, EpgAutoAddData> oldList = EpgAutoAddList;
                EpgAutoAddList = new Dictionary<uint, EpgAutoAddData>();
                var list = new List<EpgAutoAddData>();

                try { ret = CommonManager.CreateSrvCtrl().SendEnumEpgAutoAdd(ref list); } catch { ret = ErrCode.CMD_ERR; }
                if (ret != ErrCode.CMD_SUCCESS) return ret;

                list.ForEach(info => EpgAutoAddList[info.dataID] = info);

                ClearEpgAutoAddDataAppend(oldList);
                updateEpgAutoAddAppend = true;
                updateReserveAppendEpgAuto = true;
                return ret;
            });
        }

        /// <summary>自動予約登録情報の更新があれば再読み込みする</summary>
        public ErrCode ReloadManualAutoAddInfo(bool immediately = false, bool noRaiseChanged = false)
        {
            return ReloadWork(UpdateNotifyItem.AutoAddManualInfo, immediately, noRaiseChanged, ret =>
            {
                ManualAutoAddList = new Dictionary<uint, ManualAutoAddData>();
                var list = new List<ManualAutoAddData>();

                try { ret = CommonManager.CreateSrvCtrl().SendEnumManualAdd(ref list); } catch { ret = ErrCode.CMD_ERR; }
                if (ret != ErrCode.CMD_SUCCESS) return ret;

                list.ForEach(info => ManualAutoAddList[info.dataID] = info);

                manualAutoAddAppendList = null;
                updateReserveAppendManualAuto = true;
                return ret;
            });
        }
    }
}
