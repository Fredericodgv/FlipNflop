using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaceController : MonoBehaviour
{
    public enum Direction { Up, Right, Down, Left }
    public enum Corner { BottomLeft, BottomRight, TopRight, TopLeft }
    public enum TurningDirection { Clockwise, CounterClockwise }

    [Header("Movimento Retangular")]
    [Tooltip("Velocidade de movimento (unidades/segundo)")]
    public float speed = 2.0f;

    [Tooltip("Distância horizontal total (pode ser 0)")]
    public float horizontalDistance = 5.0f;

    [Tooltip("Distância vertical total (pode ser 0)")]
    public float verticalDistance = 3.0f;

    [Tooltip("Canto onde o personagem começa (a posição do objeto deve estar nesse ponto)")]
    public Corner startCorner = Corner.BottomLeft;

    [Tooltip("Sentido do trajeto ao redor do retângulo")]
    public TurningDirection turning = TurningDirection.Clockwise;


    [Tooltip("Tolerância para considerar que atingiu a quina")]
    public float cornerEpsilon = 0.001f;

    private Vector3 startPos;
    private Direction currentDirection;
    private float minX, maxX, minY, maxY;
    private Direction[] dirCycle;
    private int dirIndex;

    void Start()
    {
        startPos = transform.position; // startPos é um CANTO do retângulo
        // Define a sequência de direções conforme o sentido e o canto inicial
        Direction startDir = GetStartDirection(startCorner, turning);
        dirCycle = BuildCycle(startDir, turning);
        dirIndex = 0;
        currentDirection = dirCycle[dirIndex];

        // Define a geometria do retângulo a partir do canto em que o personagem está
        float dx = Mathf.Max(0f, horizontalDistance);
        float dy = Mathf.Max(0f, verticalDistance);

        // Determina o bottom-left (bl) a partir do canto inicial
        Vector3 bl = CornerToBottomLeft(startPos, dx, dy, startCorner);
        minX = bl.x; minY = bl.y; maxX = bl.x + dx; maxY = bl.y + dy;

        // Começa exatamente no canto (linha do perímetro)
        transform.position = startPos;
    }

    void Update()
    {
        if (horizontalDistance <= 0f && verticalDistance <= 0f || speed <= 0f)
            return;

        // Mover ao longo do eixo atual em direção à próxima quina
        Vector3 target = GetCurrentTarget();
        Vector3 pos = transform.position;

        float step = speed * Time.deltaTime;
        // Move apenas no eixo relevante
        switch (currentDirection)
        {
            case Direction.Up:
            case Direction.Down:
                pos.y = Mathf.MoveTowards(pos.y, target.y, step);
                break;
            case Direction.Right:
            case Direction.Left:
                pos.x = Mathf.MoveTowards(pos.x, target.x, step);
                break;
        }

        transform.position = pos;

        // Ao atingir a quina (ou se a aresta tiver tamanho 0), troca para o próximo eixo
        // Faz até 4 trocas no mesmo frame para cobrir casos com distâncias zero
        int safety = 0;
        while (safety++ < 4)
        {
            target = GetCurrentTarget();
            if (IsAtTarget(transform.position, target) || EdgeLengthFor(currentDirection) <= 0f)
            {
                AdvanceDirection();
                continue;
            }
            break;
        }
    }

    private Vector3 GetCurrentTarget()
    {
        switch (currentDirection)
        {
            case Direction.Up:
                return new Vector3(transform.position.x, maxY, transform.position.z);
            case Direction.Right:
                return new Vector3(maxX, transform.position.y, transform.position.z);
            case Direction.Down:
                return new Vector3(transform.position.x, minY, transform.position.z);
            case Direction.Left:
                return new Vector3(minX, transform.position.y, transform.position.z);
            default:
                return transform.position;
        }
    }

    private float EdgeLengthFor(Direction dir)
    {
        return (dir == Direction.Left || dir == Direction.Right) ? Mathf.Max(0f, horizontalDistance)
                                                                  : Mathf.Max(0f, verticalDistance);
    }

    private bool IsAtTarget(Vector3 pos, Vector3 target)
    {
        if (currentDirection == Direction.Up || currentDirection == Direction.Down)
            return Mathf.Abs(pos.y - target.y) <= cornerEpsilon;
        else
            return Mathf.Abs(pos.x - target.x) <= cornerEpsilon;
    }

    private void AdvanceDirection()
    {
        if (dirCycle == null || dirCycle.Length == 0)
        {
            // Fallback reconstrói com base no canto e sentido atuais
            Direction startDir = GetStartDirection(startCorner, turning);
            dirCycle = BuildCycle(startDir, turning);
        }
        dirIndex = (dirIndex + 1) % dirCycle.Length;
        currentDirection = dirCycle[dirIndex];
    }

    private Direction[] BuildCycle(Direction startDir, TurningDirection sense)
    {
        // Sequências base
        Direction[] baseCW = new[] { Direction.Up, Direction.Right, Direction.Down, Direction.Left };
        Direction[] baseCCW = new[] { Direction.Up, Direction.Left, Direction.Down, Direction.Right };
        var baseSeq = (sense == TurningDirection.Clockwise) ? baseCW : baseCCW;

        // Rotaciona para começar em startDir
        int idx = 0;
        for (int i = 0; i < baseSeq.Length; i++) { if (baseSeq[i] == startDir) { idx = i; break; } }
        Direction[] result = new Direction[4];
        for (int i = 0; i < 4; i++) { result[i] = baseSeq[(idx + i) % 4]; }
        return result;
    }

    private Direction GetStartDirection(Corner corner, TurningDirection sense)
    {
        // Define a primeira aresta a partir do canto e do sentido escolhido
        // Mapeamentos assumindo Y para cima e X para a direita
        if (sense == TurningDirection.Clockwise)
        {
            switch (corner)
            {
                case Corner.BottomLeft: return Direction.Up;
                case Corner.TopLeft: return Direction.Right;
                case Corner.TopRight: return Direction.Down;
                case Corner.BottomRight: return Direction.Left;
            }
        }
        else // CounterClockwise
        {
            switch (corner)
            {
                case Corner.BottomLeft: return Direction.Right;
                case Corner.BottomRight: return Direction.Up;
                case Corner.TopRight: return Direction.Left;
                case Corner.TopLeft: return Direction.Down;
            }
        }
        return Direction.Up;
    }

    private Vector3 CornerToBottomLeft(Vector3 cornerPos, float dx, float dy, Corner corner)
    {
        switch (corner)
        {
            case Corner.BottomLeft: return cornerPos;
            case Corner.BottomRight: return new Vector3(cornerPos.x - dx, cornerPos.y, cornerPos.z);
            case Corner.TopRight: return new Vector3(cornerPos.x - dx, cornerPos.y - dy, cornerPos.z);
            case Corner.TopLeft: return new Vector3(cornerPos.x, cornerPos.y - dy, cornerPos.z);
            default: return cornerPos;
        }
    }

    // startPos representa um canto inferior (BL ou BR); convertemos BR para BL via (x - dx)

    #region Gizmos
    [Header("Gizmos")]
    public bool drawPathGizmos = true;
    public Color gizmoColor = Color.yellow;
    public float gizmoCornerRadius = 0.07f;

    private void OnDrawGizmosSelected()
    {
        if (!drawPathGizmos) return;

        // Calcula o bottom-left conforme a semântica de canto inicial (start pos é um dos 4 cantos)
        Vector3 editorStart = Application.isPlaying ? startPos : transform.position;
        float dx = Mathf.Max(0f, horizontalDistance);
        float dy = Mathf.Max(0f, verticalDistance);
        Vector3 bl = CornerToBottomLeft(editorStart, dx, dy, startCorner);

        Gizmos.color = gizmoColor;

        if (dx <= 0f && dy <= 0f)
        {
            // Apenas um ponto
            Gizmos.DrawSphere(editorStart, gizmoCornerRadius);
            return;
        }

        // Calcula cantos do retângulo (pode degenerar para linha se um dos eixos for 0)
        Vector3 tl = new Vector3(bl.x, bl.y + dy, bl.z); // top-left
        Vector3 tr = new Vector3(bl.x + dx, bl.y + dy, bl.z); // top-right
        Vector3 br = new Vector3(bl.x + dx, bl.y, bl.z); // bottom-right

        // Desenha formas conforme as distâncias
        if (dx > 0f && dy > 0f)
        {
            // Retângulo completo
            Gizmos.DrawLine(bl, tl);
            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);

            Gizmos.DrawSphere(bl, gizmoCornerRadius);
            Gizmos.DrawSphere(tl, gizmoCornerRadius);
            Gizmos.DrawSphere(tr, gizmoCornerRadius);
            Gizmos.DrawSphere(br, gizmoCornerRadius);
        }
        else if (dx == 0f && dy > 0f)
        {
            // Linha vertical
            Vector3 bottom = bl; // x = anchor.x, y = anchor.y - dy
            Vector3 top = tl;    // x = anchor.x, y = anchor.y + dy
            Gizmos.DrawLine(bottom, top);
            Gizmos.DrawSphere(bottom, gizmoCornerRadius);
            Gizmos.DrawSphere(top, gizmoCornerRadius);
        }
        else if (dx > 0f && dy == 0f)
        {
            // Linha horizontal
            Vector3 left = tl;  // x = anchor.x - dx, y = anchor.y
            Vector3 right = tr; // x = anchor.x + dx, y = anchor.y
            Gizmos.DrawLine(left, right);
            Gizmos.DrawSphere(left, gizmoCornerRadius);
            Gizmos.DrawSphere(right, gizmoCornerRadius);
        }

        // Opcional: seta da direção atual (apenas em Play)
        if (Application.isPlaying)
        {
            Vector3 dir = Vector3.zero;
            switch (currentDirection)
            {
                case Direction.Up: dir = Vector3.up; break;
                case Direction.Right: dir = Vector3.right; break;
                case Direction.Down: dir = Vector3.down; break;
                case Direction.Left: dir = Vector3.left; break;
            }
            if (dir != Vector3.zero)
            {
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.6f);
                Gizmos.DrawRay(transform.position, dir * 0.5f);
            }
        }
    }
    #endregion
}
