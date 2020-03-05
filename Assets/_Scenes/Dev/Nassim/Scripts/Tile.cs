﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum TileType
{
    None,
    Free,
    Obstacle,
    Ally,
    Enemy
}

public enum TileAnim
{
    None,
    Movement,
    MovementMouseOver,
    Attack,
    AttackMouseOver
}

public class Tile : MonoBehaviour
{

    public TileType type;
    public Unit unit;
    public Animator animator;

    private Vector2 _coords;

    private List<Tile> _neighbors;

    private TileAnim _currentAnim;

    public Vector2 Coords
    {
        get => _coords;
        set 
        {
            _coords = value;
            CheckNeighbors();
        } 
    }

    // Start is called before the first frame update
    void Start()
    {
        if(animator == null)
        {
            animator = GetComponent<Animator>();
        }
        _currentAnim = TileAnim.None;
    }

    public void Appear()
    {
        animator.SetTrigger("In");
    }

    public List<Tile> GetNeighbors()
    {
        return _neighbors;
    }

    public void CheckNeighbors()
    {
        if (_neighbors == null)
        {
            _neighbors = new List<Tile>();
        }
        else
        {
            _neighbors.Clear();
        }
        if (Board.Instance.GetTile((int)_coords.x + 1, (int)_coords.y) != null)
        {
            _neighbors.Add(Board.Instance.GetTile((int)_coords.x + 1, (int)_coords.y));
        }
        if (Board.Instance.GetTile((int)_coords.x, (int)_coords.y + 1) != null)
        {
            _neighbors.Add(Board.Instance.GetTile((int)_coords.x, (int)_coords.y + 1));
        }
        if (Board.Instance.GetTile((int)_coords.x - 1, (int)_coords.y) != null)
        {
            _neighbors.Add(Board.Instance.GetTile((int)_coords.x - 1, (int)_coords.y));
        }
        if (Board.Instance.GetTile((int)_coords.x, (int)_coords.y - 1) != null)
        {
            _neighbors.Add(Board.Instance.GetTile((int)_coords.x, (int)_coords.y - 1));
        }
    }

    public bool IsNeighbor(Tile other)
    {
        return Vector2.Distance(Coords, other.Coords) == 1.0f;
    }

    public bool Equals(Tile other)
    {
        return Coords.Equals(other.Coords);
    }

    public void TriggerAnimation(TileAnim anim)
    {
        if (animator != null && _currentAnim != anim)
        {
            switch (anim)
            {
                case TileAnim.Movement:
                    animator.SetTrigger("Movement");
                    break;
                case TileAnim.MovementMouseOver:
                    animator.SetTrigger("MovementMouseOver");
                    break;
                case TileAnim.Attack:
                    animator.SetTrigger("Attack");
                    break;
                default:
                    animator.SetTrigger("None");
                    break;
            }
            _currentAnim = anim;
        }
    }

    private void OnMouseEnter() {
        RangeManager.Instance.AddToCurrentPath(this);
    }

}