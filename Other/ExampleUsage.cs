using UnityEngine;

[System.Serializable]
public class EnemySettings {
    public int hp;
    public float speed;
}

public class ExampleUsage : MonoBehaviour {
    [SerializeField, DesignFitter("スポーン位置（デザイナー調整）")]
    Transform spawn_point;


    [SerializeField, Required("プレハブが未設定です")]
    GameObject enemy_prefab;

    [SerializeField, ReadOnly]
    int cached_enemy_count;

    [SerializeField, InlineButton("Reset", "ResetEnemyCount")]
    int editor_control_value;

    [SerializeField, SceneName] string sceneName;

    [SerializeField] bool test_flag = false;

    [SerializeField, ConditionalHide("test_flag",true)] float test;


    [SerializeField, MinMaxRange(-10,20)] RangeFloat range;

    [SerializeField,Button("Range", "GetRange")]
    int dummy;


    void GetRange() {
        Debug.Log(range.Random());
    }

    public void ResetEnemyCount() {
        editor_control_value = 100;
        Debug.Log("ResetEnemyCount called");
    }
    private void Start() {
        Debug.Log(range);
        Random.Range(0, 10);
    }
}