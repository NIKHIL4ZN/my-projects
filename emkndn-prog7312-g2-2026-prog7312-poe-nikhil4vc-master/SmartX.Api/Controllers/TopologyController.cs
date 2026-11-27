using Microsoft.AspNetCore.Mvc;
using SmartX.Core.Topology.DTOs;
using SmartX.Core.Topology.Interfaces;
using SmartX.Core.Topology.Models;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/topology")]
public sealed class TopologyController : ControllerBase
{
    private readonly ITopologyService _topologyService;

    public TopologyController(ITopologyService topologyService)
    {
        _topologyService = topologyService;
    }

    [HttpGet]
    public async Task<ActionResult<TopologyResponse>> GetTopology(
        CancellationToken cancellationToken)
    {
        return Ok(await _topologyService.GetTopologyAsync(
            cancellationToken));
    }

    [HttpGet("connectivity/{gatewayId:guid}")]
    public Task<ActionResult<TopologyConnectivityResponse>>
        GetConnectivity(
            Guid gatewayId,
            [FromQuery] Guid? offlineNodeId = null,
            CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(() =>
            _topologyService.GetConnectivityAsync(
                gatewayId,
                offlineNodeId,
                cancellationToken));
    }

    [HttpGet("mst/{gatewayId:guid}")]
    public Task<ActionResult<MeshSpanningTreeResult>>
        GetSpanningTree(
            Guid gatewayId,
            [FromQuery] Guid? offlineNodeId = null,
            CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(() =>
            _topologyService.GetSpanningTreeAsync(
                gatewayId,
                offlineNodeId,
                cancellationToken));
    }

    [HttpGet("route/{gatewayId:guid}")]
    public Task<ActionResult<TopologyRouteResponse>> GetRoute(
        Guid gatewayId,
        [FromQuery] string macAddress,
        [FromQuery] bool useSpanningTree = false,
        [FromQuery] Guid? offlineNodeId = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(() =>
            _topologyService.GetRouteAsync(
                gatewayId,
                macAddress,
                useSpanningTree,
                offlineNodeId,
                cancellationToken));
    }

    private async Task<ActionResult<T>> ExecuteAsync<T>(
        Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }
}