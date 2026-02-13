using UnityEngine;

namespace HadoopCore.Scripts.Water {
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class WaterBall2D : MonoBehaviour {
        [Header("设置")] public float radius = 1f;

        public int resolution = 50; // 圆周上有多少个点 (越多越圆，但也越耗能)

        // 物理模拟参数
        [Header("果冻模拟")] public float springForce = 50f; // 2D下通常需要更大的力
        public float damping = 2f;

        private Mesh mesh;
        private Vector3[] basePositions; // 【新增】这里也是关键，记录每个点的"家"
        private Vector3[] vertices;
        private Vector3[] vertexVelocities; // 速度记录

        // 我们需要碰撞信息来触发抖动
        private Rigidbody2D rb;

        void Start() {
            rb = GetComponent<Rigidbody2D>();
            InitializeMesh();
        }

        void InitializeMesh() {
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;

            vertices = new Vector3[resolution + 1];
            basePositions = new Vector3[resolution + 1]; // 【新增】初始化
            vertexVelocities = new Vector3[vertices.Length];

            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[resolution * 3];
            vertices[0] = Vector3.zero;
            basePositions[0] = Vector3.zero; // 中心点的"家"是0,0
            uv[0] = new Vector2(0.5f, 0.5f);
            float angleStep = 360f / resolution;

            for (int i = 0; i < resolution; i++) {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                // 存下顶点位置
                Vector3 pos = new Vector3(x, y, 0);
                vertices[i + 1] = pos;
                basePositions[i + 1] = pos; // 【新增】牢牢记住这个点应该在哪！

                uv[i + 1] = new Vector2((x / radius + 1) * 0.5f, (y / radius + 1) * 0.5f);

                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;

                if (i == resolution - 1)
                    triangles[i * 3 + 2] = 1;
                else
                    triangles[i * 3 + 2] = i + 2;
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
        }

        void FixedUpdate() {
            UpdateSpringPhysics();
        }

        void UpdateSpringPhysics() {
            for (int i = 1; i < vertices.Length; i++) {
                // 【重要修复】
                // 之前的错误写法: Vector3 targetPos = vertices[i].normalized * radius;
                // 现在的正确写法: 直接读取我们要回去的"家" (basePositions)
                Vector3 targetPos = basePositions[i];
                // 计算位移: 从 当前位置 -> 目标家里的位置
                Vector3 displacement = vertices[i] - targetPos;

                // 简单的弹簧力公式
                Vector3 force = -springForce * displacement;

                // 加上阻尼防止震荡不停
                force -= vertexVelocities[i] * damping;

                // 更新速度
                vertexVelocities[i] += force * Time.fixedDeltaTime;

                // 更新位置
                vertices[i] += vertexVelocities[i] * Time.fixedDeltaTime;
            }

            // 把算好的新位置赋值给网格
            mesh.vertices = vertices;
        }

        // 当撞到墙时，产生形变
        void OnCollisionEnter2D(Collision2D collision) {
            // 找到撞击力度
            float impactForce = collision.relativeVelocity.magnitude;

            if (impactForce > 0.1f) {
                // 将撞击力传导给所有顶点
                // 一个简化的表现：把所有点往撞击的反方向推一下
                // 更好的做法是找到最近的点推，为了代码简短先全推
                Vector2 contactNormal = collision.contacts[0].normal;

                for (int i = 1; i < vertexVelocities.Length; i++) {
                    // 简单的扰动：根据点的位置和法线的关系施加力
                    // 这里的逻辑可以写得很复杂，为了演示，我们随机给点扰动
                    float randomJitter = Random.Range(0.5f, 1.5f);
                    // 把点沿着法线压扁
                    vertexVelocities[i] += (Vector3)(contactNormal * impactForce * 0.2f * randomJitter);
                }
            }
        }
    }
}