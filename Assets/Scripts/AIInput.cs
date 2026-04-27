using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class AIInput : MonoBehaviour
{
    [SerializeField] private float _minWaitTime = 0.5f;
    [SerializeField] private float _maxWaitTime = 2.0f;

    private PlayerController _controller;
    private bool _isThinking = false;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (_controller.IsAttached && !_isThinking)
        {
            StartCoroutine(WaitAndLaunch());
        }
    }

    private IEnumerator WaitAndLaunch()
    {
        _isThinking = true;

        float waitTime = Random.Range(_minWaitTime, _maxWaitTime);
        yield return new WaitForSeconds(waitTime);

        if (_controller.IsAttached)
        {
            Vector3 randomDir = Random.onUnitSphere;

            _controller.TryLaunch(randomDir);
        }

        _isThinking = false;
    }
}
