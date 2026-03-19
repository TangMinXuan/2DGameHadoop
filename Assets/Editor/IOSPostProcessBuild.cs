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

        // ---- Unity Ads ----
        "4dzt52r2t5.skadnetwork",
        "bvpn9ufa9b.skadnetwork",
        "578prtvx9j.skadnetwork",
        "7rz58n8ntl.skadnetwork",
        "ppxm28t8ap.skadnetwork",
        "v9wttpbfk9.skadnetwork",
        "n38lu8286q.skadnetwork",
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

        plist.WriteToFile(plistPath);
        Debug.Log($"[IOSPostProcessBuild] SKAdNetworkItems 写入完成，新增 {addedCount} 个 ID，共 {skAdArray.values.Count} 个");
    }
}
#endif

