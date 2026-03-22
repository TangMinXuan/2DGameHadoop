#if UNITY_IOS
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

/// <summary>
/// iOS 构建后处理脚本
/// 自动向 Info.plist 写入 SKAdNetworkItems，用于 LevelPlay(IronSource) + Unity Ads 广告归因
/// </summary>
public class IOSPostProcessBuild {

    // LevelPlay(IronSource) + Unity Ads 所需的 SKAdNetwork ID
    // 如需接入更多广告网络，在此追加对应 ID
    // 完整列表参考: https://developers.is.com/ironsource-mobile/ios/skadnetwork-support/
    private static readonly List<string> SKAdNetworkIds = new List<string> {
        // ---- IronSource / LevelPlay ----
        "su67r6k2v3.skadnetwork",
        "lr83yxwka7.skadnetwork",
        "22mmun2rn5.skadnetwork",
        "4fzdc2evr5.skadnetwork",
        "4pfyvq9l8r.skadnetwork",
        "7ug5zh24hu.skadnetwork",
        "8s468mfl3y.skadnetwork",
        "9rd848q2bz.skadnetwork",
        "9t245vhmpl.skadnetwork",
        "av6w8kgt66.skadnetwork",
        "f38h382jlk.skadnetwork",
        "hs6bdukanm.skadnetwork",
        "klf5c3l5u5.skadnetwork",
        "prcb7njmu6.skadnetwork",
        "t38b2kh725.skadnetwork",
        "v72qych5uu.skadnetwork",
        "vzmoens5gx.skadnetwork",
        "wg4vff78zm.skadnetwork",
        "yclnxrl5pm.skadnetwork",
        "zquid5ix57.skadnetwork",
        "c6k4g5qg8m.skadnetwork",
        "cstr6suwn9.skadnetwork",
        "mlmmfzh3r3.skadnetwork",
        "4468km3ulz.skadnetwork",
        "3rd42ekr43.skadnetwork",
        "kbd757ywx3.skadnetwork",
        "9g2aggbj52.skadnetwork",
        "n6fk4nfna4.skadnetwork",
        "v4nxqhlyqp.skadnetwork",
        "6g9af3uyq4.skadnetwork",
        "5lm9lj6jb7.skadnetwork",
        "252b5q1hgl.skadnetwork",
        "m8dbesm4fp.skadnetwork",
        "pwa73g5rt2.skadnetwork",

        // ---- Unity Ads XML 补充 (来自 UnityAds.xml) ----
        "2fnua5tdw4.skadnetwork",   // 
        "mj797d8u6f.skadnetwork",   // 
        "vhf287vqwu.skadnetwork",   // 
        "5tjdwbrq8w.skadnetwork",   // WEBEYE MOBILE(HK) LIMITED
        "294l99pt4k.skadnetwork",   // BidSwitch
        "mqn7fxpca7.skadnetwork",   // 
        "g6gcrrvk4p.skadnetwork",   // 
        "tl55sbb4fm.skadnetwork",   // PubNative GMBH
        "s39g8k73mm.skadnetwork",   // Bidease inc
        "a2p9lx4jpn.skadnetwork",   // Opera Software Ireland Ltd.
        "k6y4y55b64.skadnetwork",   // IGNITE MEDIA HK LIMITED
        "488r3q3dtq.skadnetwork",   // ADTIMING TECHNOLOGY PTE. LTD.
        "glqzh8vgby.skadnetwork",   // Sabio Mobile Inc.
        "97r2b46745.skadnetwork",   // 
        "zmvfpc5aq8.skadnetwork",   // Maiden Marketing Pvt Ltd.
        "3sh42y64q3.skadnetwork",   // Centro Inc.
        "2u9pt9hc89.skadnetwork",   // Remerge GmbH
        "m8dbw4sv7c.skadnetwork",   // DATASEAT LTD
        "238da6jt44.skadnetwork",   // BYTEMOD PTE. LTD-CN
        "4w7y6s5ca2.skadnetwork",   // 
        "a8cz6cu7e5.skadnetwork",   // Bigo
        "77y3x8wds4.skadnetwork",   // Anzu.io
        "n9x2a789qt.skadnetwork",   // Mail.Ru Group (myTarget)
        "f7s53z58qe.skadnetwork",   // Tencent
        "ydx93a7ass.skadnetwork",   // Adikteev SA
        "mp6xlyr22a.skadnetwork",   // Clearpier Performance Inc.
        "k674qkevps.skadnetwork",   // Pubmatic Inc
        "feyaarzu9v.skadnetwork",   // 
        "5f5u5tfb26.skadnetwork",   // INFINITE STATE PTE. LTD.
        "424m5254lk.skadnetwork",   // Snap Inc.
        "p78axxw29g.skadnetwork",   // 
        "e5fvkxwrpn.skadnetwork",   // 
        "5a6flpkh64.skadnetwork",   // REVX TECHNOLOGY PRIVATE LIMITED
        "3qy4746246.skadnetwork",   // Biga Bid Media Ltd
        "zq492l623r.skadnetwork",   // Yandex Europe AG
        "v79kvwwj4g.skadnetwork",   // Kidoz Ltd.
        "44jx6755aq.skadnetwork",   // Persona.ly LTD
        "32z4fx6l9h.skadnetwork",   // Click Tech Limited
        "w9q455wk68.skadnetwork",   // hybrid.ai
        "xga6mpmplv.skadnetwork",   // 
        "x44k69ngh6.skadnetwork",   // Clearpier Inc.
        "f73kdq92p3.skadnetwork",   // SPOTAD LTD
        "5l3tpt7t6e.skadnetwork",   // 
        "wzmmz9fp6w.skadnetwork",   // mkhoj Solutions Private Limited
        "9nlqeag3gk.skadnetwork",   // 
        "6yxyv74ff7.skadnetwork",   // TOPON PTE. LTD.

        // ---- Unity Ads ----
        "4dzt52r2t5.skadnetwork",
        "bvpn9ufa9b.skadnetwork",
        "578prtvx9j.skadnetwork",
        "7rz58n8ntl.skadnetwork",
        "ppxm28t8ap.skadnetwork",
        "v9wttpbfk9.skadnetwork",
        "n38lu8286q.skadnetwork",
        "uw77j35x4d.skadnetwork",
    };

    // 优先级 999 保证在其他 PostProcessBuild 脚本之后执行，防止被覆盖
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget target, string buildPath) {
        if (target != BuildTarget.iOS) return;

        string plistPath = Path.Combine(buildPath, "Info.plist");
        if (!File.Exists(plistPath)) {
            Debug.LogWarning("[IOSPostProcessBuild] Info.plist 未找到，路径: " + plistPath);
            return;
        }

        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        var root = plist.root;

        // 获取已有数组，或新建
        PlistElementArray skAdArray;
        if (root.values.ContainsKey("SKAdNetworkItems")) {
            skAdArray = root.values["SKAdNetworkItems"].AsArray();
        } else {
            skAdArray = root.CreateArray("SKAdNetworkItems");
        }

        // 收集已有 ID，避免重复写入
        var existingIds = new HashSet<string>();
        foreach (var element in skAdArray.values) {
            var dict = element.AsDict();
            if (dict != null && dict.values.ContainsKey("SKAdNetworkIdentifier")) {
                existingIds.Add(dict.values["SKAdNetworkIdentifier"].AsString());
            }
        }

        // 追加缺失的 ID
        int addedCount = 0;
        foreach (var id in SKAdNetworkIds) {
            if (!existingIds.Contains(id)) {
                var item = skAdArray.AddDict();
                item.SetString("SKAdNetworkIdentifier", id);
                addedCount++;
            }
        }

        // Google AdMob App ID（必须填写，否则 App 启动会 Crash）
        // 在 AdMob 后台 → 应用 → 应用设置 中获取，格式: ca-app-pub-XXXXXXXXXXXXXXXX~YYYYYYYYYY
        root.SetString("GADApplicationIdentifier", "ca-app-pub-5455174213489746~3910316929");

        plist.WriteToFile(plistPath);
        Debug.Log($"[IOSPostProcessBuild] SKAdNetworkItems 写入完成，新增 {addedCount} 个 ID，共 {skAdArray.values.Count} 个");
    }
}
#endif

