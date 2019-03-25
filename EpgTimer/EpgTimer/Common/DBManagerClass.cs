using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace EpgTimer
{
    public class EpgServiceAllEventInfo : EpgServiceEventInfo
    {
        public List<EpgEventInfo> eventArcList;
        public IEnumerable<EpgEventInfo> eventMergeList { get { return eventArcList.Concat(eventList); } }
        public EpgServiceAllEventInfo(EpgServiceInfo serviceInfo, List<EpgEventInfo> eventList = null, List<EpgEventInfo> eventArcList = null)
        {
            this.serviceInfo = serviceInfo;
            this.eventList = eventList ?? new List<EpgEventInfo>();
            this.eventArcList = eventArcList ?? new List<EpgEventInfo>();
        }
        public static Dictionary<UInt64, EpgServiceAllEventInfo> CreateEpgServiceDictionary(List<EpgServiceEventInfo> list, List<EpgServiceEventInfo> list2, bool reuseEvent = false)
        {
            var serviceDic = new Dictionary<UInt64, EpgServiceAllEventInfo>();
            foreach (EpgServiceEventInfo info in list)
            {
                UInt64 id = info.serviceInfo.Key;
                //対応する過去番組情報があれば付加する
                int i = list2.FindIndex(info2 => id == info2.serviceInfo.Key);
                serviceDic[id] = new EpgServiceAllEventInfo(info.serviceInfo, info.eventList, i < 0 ? new List<EpgEventInfo>() : list2[i].eventList);
            }
            //過去番組情報が残っていればサービスリストに加える
            foreach (EpgServiceEventInfo info in list2)
            {
                UInt64 id = info.serviceInfo.Key;
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
        public Dictionary<UInt64, EpgEventInfo> EventUIDList { get; private set; }//検索用インデックス
        public DateTime EventTimeMin { get { return CommonUtil.Min(EventTimeMinArc, EventTimeMinCurrent); } }
        public DateTime EventTimeMinArc { get; private set; }
        public DateTime EventTimeMaxArc { get; private set; }
        private DateTime EventTimeBaseArc; //現在読み込まれている過去番組期間の開始
        private DateTime EventTimeMinCurrent;
        public Dictionary<UInt32, ReserveData> ReserveList { get; private set; }
        public Dictionary<UInt32, TunerReserveInfo> TunerReserveList { get; private set; }
        //public RecSettingData DefaultRecSetting { get; private set; }
        public Dictionary<UInt32, RecFileInfo> RecFileInfo { get; private set; }
        public Dictionary<UInt64, List<RecFileInfo>> RecFileUIDList { get; private set; }//録画結果のUIDはかぶることがある
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
                            //イベントの再利用。再利用不可の場合でもサービス名の修正は現在番組なので不用。
                            if (IsEpgLoaded)
                            {
                                for (int j = 0; j < list_list[i].Count; j++)
                                {
                                    EpgEventInfo refData;
                                    if (EventUIDList.TryGetValue(list_list[i][j].CurrentPgUID(), out refData))
                                    {
                                        list_list[i][j] = refData;
                                    }
                                }
                            }
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

        public EpgEventInfo GetReserveEventList(ReserveData master, bool isSrv = true)
        {
            if (master == null) return null;

            if (reserveEventList == null)
            {
                if (IsEpgLoaded || Settings.Instance.NoReserveEventList == true)
                {
                    reserveEventList = ReserveList.Values.ToDictionary(rs => rs.ReserveID, rs => MenuUtil.SearchEventInfoLikeThat(rs));
                }
                else
                {
                    if (isSrv == false || master.IsManual) return null;

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
            EventUIDList = new Dictionary<ulong, EpgEventInfo>();
            EventTimeMinArc = DateTime.MaxValue;
            EventTimeMaxArc = DateTime.MinValue;
            EventTimeBaseArc = DateTime.MaxValue;
            EventTimeMinCurrent = DateTime.MaxValue;
            ReserveList = new Dictionary<uint, ReserveData>();
            TunerReserveList = new Dictionary<uint, TunerReserveInfo>();
            //DefaultRecSetting = null;
            RecFileInfo = new Dictionary<uint, RecFileInfo>();
            RecFileUIDList = new Dictionary<UInt64, List<RecFileInfo>>();
            RecNamePlugInList = new List<string>();
            WritePlugInList = new List<string>();
            ManualAutoAddList = new Dictionary<uint, ManualAutoAddData>();
            EpgAutoAddList = new Dictionary<uint, EpgAutoAddData>();
        }

        public bool IsEpgLoaded { get { return EventTimeMinCurrent != DateTime.MaxValue; } }
        public bool IsEventTimePossible(DateTime time)
        {
            //3項目は、過去番組データが無く現在データも読み込まれていない場合で通信に問題がある場合も含む。
            //なお、データ収集の方法によってはEventTimeMinCurrent<EventTimeMinArcの可能性もゼロではないが、
            //稀なケースと思われるので無視する。
            return EventTimeMin <= time || IsEpgLoaded == false && EventTimeMinArc == DateTime.MaxValue;
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
                EventTimeMinCurrent = DateTime.MaxValue;
            }
            else if(notify == UpdateNotifyItem.EpgDatabaseInfo)
            {
                EventTimeMinArc = DateTime.MaxValue;
                EventTimeMaxArc = DateTime.MinValue;
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
                EventUIDList = new Dictionary<ulong, EpgEventInfo>();
                EventTimeBaseArc = DateTime.MaxValue;

                var list = new List<EpgServiceEventInfo>();
                try { ret = CommonManager.CreateSrvCtrl().SendEnumPgAll(ref list); } catch { ret = ErrCode.CMD_ERR; }
                //SendEnumPgAll()は番組情報未取得状態でCMD_ERRを返す。従来エラー扱いだったが、取得数0の成功とみなす
                if (ret != ErrCode.CMD_SUCCESS && ret != ErrCode.CMD_ERR) return ret;

                var list2 = new List<EpgServiceEventInfo>();
                EventTimeBaseArc = EpgViewPeriod.DefPeriod.StartLoad;
                LoadEpgArcData(EventTimeBaseArc, DateTime.MaxValue, ref list2);

                //リストの作成
                EventTimeMinCurrent = list.SelectMany(info => info.eventList).Min(data => data.PgStartTime);
                ServiceEventList = EpgServiceAllEventInfo.CreateEpgServiceDictionary(list, list2);
                CorrectServiceInfo(ServiceEventList.Values);
                foreach (var data in ServiceEventList.Values.SelectMany(info => info.eventMergeList))
                {
                    EventUIDList[data.CurrentPgUID()] = data;//通常あり得ないがUID被りは後優先。
                }

                //リモコンIDの登録
                ChSet5.SetRemoconID(ServiceEventList.Select(item => item.Value.serviceInfo));

                reserveEventList = null;
                reserveEventListCache = null;

                return ErrCode.CMD_SUCCESS;
            });
        }
        public ErrCode ReloadEpgDatabaseInfo(bool immediately = false, bool noRaiseChanged = false)
        {
            return ReloadWork(UpdateNotifyItem.EpgDatabaseInfo, immediately, noRaiseChanged, ret =>
            {
                var mm = new List<long>();
                try { ret = CommonManager.CreateSrvCtrl().SendGetPgArcMinMax(new List<long> { 0xFFFFFFFFFFFF, 0xFFFFFFFFFFFF }, ref mm); } catch { ret = ErrCode.CMD_ERR; }
                if (ret != ErrCode.CMD_SUCCESS) return ret;

                //全過去番組情報の最小開始時間
                EventTimeMinArc = mm[0] != long.MaxValue ? DateTime.FromFileTimeUtc(mm[0]) : DateTime.MaxValue;
                EventTimeMaxArc = mm[1] != long.MinValue ? DateTime.FromFileTimeUtc(mm[1]) : DateTime.MinValue;

                return ret;
            });
        }
        //過去番組情報用の補正
        public void CorrectServiceInfo(IEnumerable<EpgServiceEventInfo> list, bool reUseData = false)
        {
            //データ未ロード時は再利用不可
            reUseData &= IsEpgLoaded;

            foreach (EpgServiceEventInfo info in list)
            {
                //あれば取得EPGデータのEpgServiceInfo、EventInfoに差し替え
                EpgServiceAllEventInfo refInfo;
                if (reUseData && ServiceEventList.TryGetValue(info.serviceInfo.Key, out refInfo))
                {
                    info.serviceInfo = refInfo.serviceInfo;
                }
                else if (info.serviceInfo.TSID != ChSet5.ChItem(info.serviceInfo.Key, true, true).TSID)
                {
                    info.serviceInfo.service_name = "[廃]" + info.serviceInfo.service_name;
                }

                new List<List<EpgEventInfo>> { info.eventList, info is EpgServiceAllEventInfo ? (info as EpgServiceAllEventInfo).eventArcList : new List<EpgEventInfo>() }
                .ForEach(eventList =>
                {
                    for (int i = 0; i < eventList.Count; i++)
                    {
                        EpgEventInfo refData;
                        if (reUseData && EventUIDList.TryGetValue(eventList[i].CurrentPgUID(), out refData))
                        {
                            eventList[i] = refData;
                        }
                        else
                        {
                            eventList[i].ServiceName = info.serviceInfo.service_name;
                        }
                    }
                });
            }
        }

        /// <summary>現在の取得データに合わせてデフォルト表示の番組情報を展開する</summary>
        public List<UInt64> ExpandSpecialKey(List<UInt64> keyList, IEnumerable<EpgServiceInfo> additionalInfo = null)
        {
            if (keyList.All(key => !EpgServiceInfo.IsSPKey(key))) return keyList;

            var list1 = Settings.Instance.ShowEpgCapServiceOnly ? ChSet5.ChListSelected :
                 (ServiceEventList.Any() ? ServiceEventList.Values.Select(info => info.serviceInfo) : ChSet5.ChList.Values)
                    .Concat(additionalInfo ?? Enumerable.Empty<EpgServiceInfo>());

            List<EpgServiceInfo> infoList = ChSet5.GetSortedChList(list1.Distinct(), true, true).ToList();
            var exDic = new Dictionary<UInt64, UInt64[]>();
            exDic.Add((UInt64)EpgServiceInfo.SpecialViewServices.ViewServiceDttv, infoList.Where(info => info.IsDttv).Select(info => info.Key).ToArray());
            exDic.Add((UInt64)EpgServiceInfo.SpecialViewServices.ViewServiceBS, infoList.Where(info => info.IsBS).Select(info => info.Key).ToArray());
            exDic.Add((UInt64)EpgServiceInfo.SpecialViewServices.ViewServiceCS, infoList.Where(info => info.IsCS).Select(info => info.Key).ToArray());
            exDic.Add((UInt64)EpgServiceInfo.SpecialViewServices.ViewServiceCS3, infoList.Where(info => info.IsSPHD).Select(info => info.Key).ToArray());
            exDic.Add((UInt64)EpgServiceInfo.SpecialViewServices.ViewServiceOther, infoList.Where(info => info.IsOther).Select(info => info.Key).ToArray());

            var exList = new List<UInt64>();
            foreach (UInt64 key in keyList)
            {
                if(exDic.ContainsKey(key))//一応チェック
                {
                    exList.AddRange(exDic[key]);
                }
                else
                {
                    exList.Add(key);
                }
            }
            return exList.Distinct().ToList();
        }

        public ErrCode LoadEpgData(ref Dictionary<UInt64, EpgServiceAllEventInfo> serviceDic, EpgViewPeriod period = null, IEnumerable<ulong> keys = null)
        {
            ErrCode err = ReloadEpgData();
            if (err != ErrCode.CMD_SUCCESS) return err;

            bool noCurrent = period != null && IsEpgLoaded && period.End <= EventTimeMinCurrent;
            bool noArc = period == null || EventTimeBaseArc != DateTime.MaxValue && period.End <= EventTimeBaseArc;

            serviceDic = ServiceEventList.ToDictionary(item => item.Key,
                item => new EpgServiceAllEventInfo(item.Value.serviceInfo
                , noCurrent ? null : PeriodApplied(item.Value.eventList, period.StrictLoad ? period : null)
                , noArc ? null : PeriodApplied(item.Value.eventArcList, period.StrictLoad ? period : null)));

            var list = new List<EpgServiceEventInfo>();
            if (period != null && period.StartLoad < EventTimeBaseArc)
            {
                err = LoadEpgArcData(period.StartLoad, CommonUtil.Min(period.End, EventTimeBaseArc), ref list, keys);
                if (err != ErrCode.CMD_SUCCESS) return err;

                //リモコンIDの登録、サービス名の補正
                ChSet5.SetRemoconID(list.Select(info => info.serviceInfo), true);
                CorrectServiceInfo(list);

                foreach (EpgServiceEventInfo info in list)
                {
                    UInt64 id = info.serviceInfo.Key;
                    if (serviceDic.ContainsKey(id))
                    {
                        info.eventList.AddRange(serviceDic[id].eventArcList);
                        serviceDic[id].eventArcList = info.eventList;
                    }
                    else
                    {
                        serviceDic[id] = new EpgServiceAllEventInfo(info.serviceInfo, new List<EpgEventInfo>(), info.eventList);
                    }
                }
            }

            //イベントリストが空の場合もあるが、そのまま返す。
            return err;
        }
        /// <summary>SPKeyが無ければ指定サービスのみ過去EPGを読み込む</summary>
        public ErrCode LoadEpgArcData(DateTime start, DateTime end, ref List<EpgServiceEventInfo> list, IEnumerable<ulong> keys = null)
        {
            try
            {
                List<long> keyList;
                if (keys == null || keys.Any(key => EpgServiceInfo.IsSPKey(key)))
                {
                    keyList = new List<long> { 0xFFFFFFFFFFFF, 0xFFFFFFFFFFFF };
                }
                else
                {
                    keyList = keys.Select(key => new List<long> { 0, (long)key }).SelectMany(lst => lst).ToList();
                }
                //EDCB系の時刻は、UTCじゃないけどUTC扱いなので
                keyList.Add(start.ToFileTimeUtc());
                keyList.Add(end == DateTime.MaxValue ? long.MaxValue : end.ToFileTimeUtc());

                return CommonManager.CreateSrvCtrl().SendEnumPgArc(keyList, ref list);
            }
            catch { return ErrCode.CMD_ERR; }
        }
        private List<EpgEventInfo> PeriodApplied(List<EpgEventInfo> list, EpgViewPeriod period)
        {
            if (period == null) return list.ToList();
            bool needNow = period.End >= DateTime.UtcNow.AddHours(9);
            return list.FindAll(d => needNow && d.StartTimeFlag == 0 || period.Contains(d.PgStartTime));
        }

        //過去番組も含めた検索
        public List<EpgEventInfo> SearchPg(List<EpgSearchKeyInfo> key, EpgViewPeriod period = null)
        {
            //サービス名の補正など行うためにSearchPgLists()から作成する。
            Dictionary<UInt64, EpgServiceAllEventInfo> dic = null;
            SearchPgLists(key, ref dic, period);
            return dic == null ? new List<EpgEventInfo>() : dic.Values.SelectMany(info => info.eventMergeList).ToList();
        }
        public ErrCode SearchPgLists(List<EpgSearchKeyInfo> key, ref Dictionary<UInt64, EpgServiceAllEventInfo> serviceDic, EpgViewPeriod period = null)
        {
            ErrCode err = ErrCode.CMD_SUCCESS;

            //Epgデータ未取得時、SendSearchPg()の最古データは取得してみないと分からない。
            var list = new List<EpgEventInfo>();
            bool noSearchCurrent = period != null && IsEpgLoaded && period.End <= EventTimeMinCurrent;
            if (noSearchCurrent == false)
            {
                try { err = CommonManager.CreateSrvCtrl().SendSearchPg(key, ref list); } catch { err = ErrCode.CMD_ERR; }
                if (err != ErrCode.CMD_SUCCESS) return err;
                if (period != null && period.StrictLoad) list = PeriodApplied(list, period);
            }

            var list2 = new List<EpgEventInfo>();
            bool noSearchArc = period == null || EventTimeMaxArc != DateTime.MinValue && period.StartLoad > EventTimeMaxArc;
            if (noSearchArc == false)
            {
                try
                {
                    var pram = new SearchPgParam();
                    pram.keyList = key;
                    pram.enumStart = period.StartLoad.ToFileTimeUtc();
                    pram.enumEnd = period.End == DateTime.MaxValue ? long.MaxValue : period.End.ToFileTimeUtc();
                    CommonManager.CreateSrvCtrl().SendSearchPgArc(pram, ref list2);
                }
                catch { }
            }

            //サービス毎のリストに変換
            var sList = list.GroupBy(info => info.Create64Key()).Select(gr => new EpgServiceEventInfo { serviceInfo = EpgServiceInfo.FromKey(gr.Key), eventList = gr.ToList() }).ToList();
            var sList2 = list2.GroupBy(info => info.Create64Key()).Select(gr => new EpgServiceEventInfo { serviceInfo = EpgServiceInfo.FromKey(gr.Key), eventList = gr.ToList() }).ToList();
            serviceDic = EpgServiceAllEventInfo.CreateEpgServiceDictionary(sList, sList2);

            //サービス名の補正、イベントデータの再使用
            CorrectServiceInfo(serviceDic.Values, period == null || EventTimeBaseArc < period.End || EventTimeMinCurrent < period.End);

            return err;
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
                RecFileUIDList = new Dictionary<UInt64, List<RecFileInfo>>();
                var list = new List<RecFileInfo>();

                try { ret = CommonManager.CreateSrvCtrl().SendEnumRecInfoBasic(ref list); } catch { ret = ErrCode.CMD_ERR; }
                if (ret != ErrCode.CMD_SUCCESS) return ret;

                list.ForEach(info => RecFileInfo[info.ID] = info);

                //追加の検索用リスト
                RecFileUIDList = list.GroupBy(item => item.CurrentPgUID()).ToDictionary(gr => gr.Key, gr => gr.ToList());

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
