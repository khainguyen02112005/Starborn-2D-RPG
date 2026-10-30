using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float speed = 2.5f; // Tốc độ chạy
    public float aggroRadius = 5f; // Tầm nhìn
    public float attackCooldown = 1.5f; // Quái mất 1.5 giây để cắn nhát tiếp theo
    public int contactDamage = 1;
    private float nextAttackTime = 0f;
    private Transform player;
    private Vector2 startPosition; // Biến lưu địa chỉ nhà cũ
    private Rigidbody2D rb;
    private EnemySPUMAnimation animationDriver;
    private readonly PathFollower navigation = new PathFollower();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animationDriver = GetComponent<EnemySPUMAnimation>();
        if (animationDriver == null) animationDriver = gameObject.AddComponent<EnemySPUMAnimation>();
        if (FindAnyObjectByType<MapSceneLayout>() != null && GetComponent<MapDepthSort>() == null)
            gameObject.AddComponent<MapDepthSort>();
        
        // Ghi nhớ vị trí lúc vừa xuất hiện làm nhà
        startPosition = transform.position; 

        // Tìm người chơi
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void FixedUpdate() // Dùng FixedUpdate để xử lý vật lý mượt hơn
    {
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (GameplayUiFocus.BlocksControls || (health != null && health.IsMovementSuppressed))
        {
            animationDriver?.SetMoving(false);
            return;
        }
        if (player == null)
        {
            animationDriver?.SetMoving(false);
            return;
        }

        // Tính khoảng cách
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= aggroRadius)
        {
            // NẾU THẤY NGƯỜI CHƠI -> RƯỢT!
            Vector2 targetPos = navigation.NextPosition(rb.position, player.position, speed * Time.fixedDeltaTime);
            rb.MovePosition(targetPos);
            
            FlipSprite(player.position.x); // Quay mặt về hướng người chơi
            animationDriver?.SetMoving(Vector2.Distance(rb.position, targetPos) > .001f);
        }
        else
        {
            // NẾU NGƯỜI CHƠI ĐI KHUẤT -> QUAY VỀ NHÀ
            Vector2 targetPos = navigation.NextPosition(rb.position, startPosition, speed * Time.fixedDeltaTime);
            rb.MovePosition(targetPos);
            
            FlipSprite(startPosition.x); // Quay mặt về hướng nhà
            animationDriver?.SetMoving(Vector2.Distance(rb.position, targetPos) > .001f);
        }
    }

    // Shared 2D ground navigation; existing enemy/boss components reuse it.
    public sealed class PathFollower
    {
        private System.Collections.Generic.List<Vector2> route;
        private Vector2 lastGoal;
        private float nextPlan;
        private int waypoint;
        public Vector2 NextPosition(Vector2 from, Vector2 target, float distance, float radius = .28f)
        {
            if (route == null || Time.time >= nextPlan || Vector2.Distance(lastGoal, target) > .8f)
            {
                route = FindRoute(from, target, radius); waypoint = 0; lastGoal = target;
                nextPlan = Time.time + .55f + Random.value * .15f;
            }
            while (route != null && waypoint < route.Count && Vector2.Distance(from, route[waypoint]) < .12f) waypoint++;
            if (route == null || waypoint >= route.Count) return from;
            var layout = FindAnyObjectByType<MapSceneLayout>();
            if (layout != null && !layout.IsWalkable(from, radius) && layout.IsWalkable(route[waypoint], radius)) return route[waypoint];
            Vector2 next = Vector2.MoveTowards(from, route[waypoint], distance);
            return ClearLine(from, next, radius) ? next : from;
        }
    }

    private static readonly System.Collections.Generic.Dictionary<int, NavigationGrid> grids = new System.Collections.Generic.Dictionary<int, NavigationGrid>();
    public static System.Collections.Generic.List<Vector2> FindRoute(Vector2 from, Vector2 target, float radius = .28f)
    {
        var layout = FindAnyObjectByType<MapSceneLayout>();
        if (layout == null) return ClearLine(from, target, radius) ? new System.Collections.Generic.List<Vector2> { target } : new System.Collections.Generic.List<Vector2>();
        if (ClearLine(from, target, radius) && layout.IsWalkable(target, radius)) return new System.Collections.Generic.List<Vector2> { target };
        int key = Mathf.RoundToInt(radius * 100);
        if (!grids.TryGetValue(key, out NavigationGrid grid) || grid.layout != layout) grids[key] = grid = new NavigationGrid(layout, radius);
        return grid.Route(from, target);
    }
    public static bool ClearLine(Vector2 from, Vector2 to, float radius = .28f)
    {
        var layout = FindAnyObjectByType<MapSceneLayout>();
        int mask = layout != null ? layout.solidLayers.value : 1 << 8;
        foreach (var hit in Physics2D.CircleCastAll(from, radius, to - from, Vector2.Distance(from, to), mask))
            if (hit.collider != null && !hit.collider.isTrigger) return false;
        if (layout != null)
        {
            int samples = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / .3f));
            for (int i = 0; i <= samples; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, i / (float)samples);
                if (!layout.walkableBounds.Contains(point) || layout.ground != null && !layout.ground.HasTile(layout.ground.WorldToCell(point))) return false;
            }
        }
        return true;
    }
    private sealed class NavigationGrid
    {
        public readonly MapSceneLayout layout;
        private readonly float radius;
        private readonly int width, height;
        private readonly Vector2 origin;
        private sbyte[] cached;
        private float expires;
        const float Cell = .5f;
        public NavigationGrid(MapSceneLayout source, float clearance)
        {
            layout = source; radius = clearance; origin = source.walkableBounds.min;
            width = Mathf.CeilToInt(source.walkableBounds.width / Cell); height = Mathf.CeilToInt(source.walkableBounds.height / Cell);
            cached = new sbyte[width * height];
        }
        Vector2 Point(int i) => origin + new Vector2(i % width + .5f, i / width + .5f) * Cell;
        bool Free(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return false;
            int i = y * width + x;
            if (cached[i] == 0) cached[i] = (sbyte)(layout.IsWalkable(Point(i), radius) ? 1 : -1);
            return cached[i] > 0;
        }
        int Nearest(Vector2 p)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt((p.x - origin.x) / Cell), 0, width - 1), y = Mathf.Clamp(Mathf.FloorToInt((p.y - origin.y) / Cell), 0, height - 1);
            for (int ring = 0; ring < 8; ring++)
                for (int dy = -ring; dy <= ring; dy++) for (int dx = -ring; dx <= ring; dx++)
                    if ((Mathf.Abs(dx) == ring || Mathf.Abs(dy) == ring) && Free(x + dx, y + dy)) return (y + dy) * width + x + dx;
            return -1;
        }
        public System.Collections.Generic.List<Vector2> Route(Vector2 from, Vector2 goal)
        {
            if (Time.time >= expires) { System.Array.Clear(cached, 0, cached.Length); expires = Time.time + .8f; }
            int start = Nearest(from), end = Nearest(goal); var result = new System.Collections.Generic.List<Vector2>();
            if (start < 0 || end < 0) return result;
            var costs = new float[cached.Length]; var previous = new int[cached.Length]; var closed = new bool[cached.Length];
            for (int i = 0; i < costs.Length; i++) { costs[i] = float.PositiveInfinity; previous[i] = -1; }
            var open = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, float>>();
            costs[start] = 0; Push(open, start, Vector2.Distance(Point(start), Point(end)));
            while (open.Count > 0)
            {
                int current = Pop(open); if (closed[current]) continue; closed[current] = true;
                if (current == end) break;
                int cx = current % width, cy = current / width;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0 || !Free(cx + dx, cy + dy)) continue;
                    if (dx != 0 && dy != 0 && (!Free(cx + dx, cy) || !Free(cx, cy + dy))) continue;
                    int next = (cy + dy) * width + cx + dx;
                    float cost = costs[current] + (dx != 0 && dy != 0 ? .707107f : Cell);
                    if (closed[next] || cost >= costs[next]) continue;
                    costs[next] = cost; previous[next] = current;
                    Push(open, next, cost + Vector2.Distance(Point(next), Point(end)));
                }
            }
            if (!closed[end]) return result;
            for (int i = end; i != start && i >= 0; i = previous[i]) result.Add(Point(i));
            result.Reverse();
            if (!layout.IsWalkable(from, radius)) result.Insert(0, Point(start));
            for (int i = 0; i + 1 < result.Count;)
                if (ClearLine(i == 0 ? from : result[i - 1], result[i + 1], radius)) result.RemoveAt(i); else i++;
            if (ClearLine(result.Count > 0 ? result[result.Count - 1] : from, goal, radius) && layout.IsWalkable(goal, radius)) result.Add(goal);
            return result;
        }
        static void Push(System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, float>> heap, int id, float priority)
        {
            var item = new System.Collections.Generic.KeyValuePair<int, float>(id, priority); heap.Add(item); int i = heap.Count - 1;
            while (i > 0) { int parent = (i - 1) / 2; if (heap[parent].Value <= priority) break; heap[i] = heap[parent]; i = parent; } heap[i] = item;
        }
        static int Pop(System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, float>> heap)
        {
            int id = heap[0].Key; var item = heap[heap.Count - 1]; heap.RemoveAt(heap.Count - 1); int i = 0;
            while (i * 2 + 1 < heap.Count) { int child = i * 2 + 1; if (child + 1 < heap.Count && heap[child + 1].Value < heap[child].Value) child++; if (heap[child].Value >= item.Value) break; heap[i] = heap[child]; i = child; }
            if (heap.Count > 0) heap[i] = item; return id;
        }
    }

    // Hàm lật mặt (Sửa lỗi quay mặt giật giật khi đứng sát nhau)
    void FlipSprite(float targetX)
    {
        if (Mathf.Abs(targetX - transform.position.x) > 0.1f)
        {
            float currentScale = Mathf.Abs(transform.localScale.y); // Giữ nguyên kích thước hiện tại (để quái Tinh Anh không bị teo lại)
            if (targetX > transform.position.x)
                transform.localScale = new Vector3(-currentScale, currentScale, currentScale); // Xoay phải
            else 
                transform.localScale = new Vector3(currentScale, currentScale, currentScale);  // Xoay trái
        }
    }
    
    // Vẽ vòng đỏ trong Scene để dễ canh tầm nhìn
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
    }
        // Dùng OnCollisionStay2D để dù có đứng dính chặt vào nhau nó vẫn cắn đặn đặn
    void OnCollisionStay2D(Collision2D collision)
    {
        // Unity can deliver collision messages to a disabled MonoBehaviour.
        if (!enabled || GameplayUiFocus.BlocksControls || (TryGetComponent(out EnemyHealth health) && health.IsMovementSuppressed)) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            // Kiểm tra xem đã hết thời gian chờ (cooldown) chưa
            if (Time.time >= nextAttackTime)
            {
                PlayerHealth playerHP = collision.gameObject.GetComponent<PlayerHealth>();
                if (playerHP != null)
                {
                    animationDriver?.PlayPhysicalAttack();
                    playerHP.TakeDamage(contactDamage);
                    nextAttackTime = Time.time + attackCooldown; // Hẹn giờ nhát cắn tiếp theo
                    Debug.Log("Quái vừa cắn bạn!");
                }
            }
        }
    }
}
