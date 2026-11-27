using SmartX.Core.DTOs;
using SmartX.Core.Enums;
using SmartX.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartX.Core.Validation
{
    public class DeploymentTreeValidator
     : IDeploymentTreeValidator
    {
        private const int MaximumAllowedDepth = 8;

        private static readonly Regex MacAddressPattern = new(
            @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$",
            RegexOptions.Compiled);

        public DeploymentValidationResult Validate(
            DeploymentNodeRequest root)
        {
            ArgumentNullException.ThrowIfNull(root);

            var result = new DeploymentValidationResult();

            if (root.NodeType != DeploymentNodeType.Facility)
            {
                result.Errors.Add(
                    "The deployment root must be a Facility.");
            }

            ValidateNodeRecursively(
                root,
                parentType: null,
                depth: 0,
                path: root.Name,
                result);

            return result;
        }

        private static void ValidateNodeRecursively(
            DeploymentNodeRequest node,
            DeploymentNodeType? parentType,
            int depth,
            string path,
            DeploymentValidationResult result)
        {
            result.NodesVisited++;
            result.MaximumDepthReached =
                Math.Max(result.MaximumDepthReached, depth);

            if (depth > MaximumAllowedDepth)
            {
                result.Errors.Add(
                    $"{path}: deployment depth exceeds " +
                    $"{MaximumAllowedDepth} levels.");

                return;
            }

            if (string.IsNullOrWhiteSpace(node.Name))
            {
                result.Errors.Add(
                    $"{path}: every node requires a name.");
            }

            if (!Enum.IsDefined(node.NodeType))
            {
                result.Errors.Add(
                    $"{path}: node type is invalid.");

                return;
            }

            if (parentType.HasValue
                && !IsAllowedChild(
                    parentType.Value,
                    node.NodeType))
            {
                result.Errors.Add(
                    $"{path}: a {node.NodeType} cannot be " +
                    $"placed directly inside a {parentType.Value}.");
            }

            ValidateSensorConfiguration(
                node,
                path,
                result);

            ValidateUniqueChildNames(
                node,
                path,
                result);

            foreach (var child in node.Children)
            {
                if (child is null)
                {
                    result.Errors.Add(
                        $"{path}: null child nodes are not allowed.");

                    continue;
                }

                var childPath =
                    $"{path} > {child.Name}";

                ValidateNodeRecursively(
                    child,
                    node.NodeType,
                    depth + 1,
                    childPath,
                    result);
            }
        }

        private static void ValidateSensorConfiguration(
            DeploymentNodeRequest node,
            string path,
            DeploymentValidationResult result)
        {
            if (node.NodeType == DeploymentNodeType.Sensor)
            {
                if (string.IsNullOrWhiteSpace(
                    node.SensorMacAddress)
                    || !MacAddressPattern.IsMatch(
                        node.SensorMacAddress))
                {
                    result.Errors.Add(
                        $"{path}: sensor nodes require a valid " +
                        "MAC address.");
                }

                if (node.Children.Count > 0)
                {
                    result.Errors.Add(
                        $"{path}: sensor nodes cannot contain " +
                        "child nodes.");
                }

                return;
            }

            if (!string.IsNullOrWhiteSpace(
                node.SensorMacAddress))
            {
                result.Errors.Add(
                    $"{path}: only sensor nodes may define " +
                    "a MAC address.");
            }
        }

        private static void ValidateUniqueChildNames(
            DeploymentNodeRequest node,
            string path,
            DeploymentValidationResult result)
        {
            var names = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var child in node.Children)
            {
                if (child is not null
                    && !names.Add(child.Name.Trim()))
                {
                    result.Errors.Add(
                        $"{path}: duplicate child name " +
                        $"'{child.Name}'.");
                }
            }
        }

        private static bool IsAllowedChild(
            DeploymentNodeType parent,
            DeploymentNodeType child)
        {
            return parent switch
            {
                DeploymentNodeType.Facility =>
                    child is DeploymentNodeType.Zone
                        or DeploymentNodeType.Gateway,

                DeploymentNodeType.Zone =>
                    child is DeploymentNodeType.SubZone
                        or DeploymentNodeType.Gateway
                        or DeploymentNodeType.Sensor,

                DeploymentNodeType.SubZone =>
                    child is DeploymentNodeType.Gateway
                        or DeploymentNodeType.Sensor,

                DeploymentNodeType.Gateway =>
                    child == DeploymentNodeType.Sensor,

                DeploymentNodeType.Sensor => false,

                _ => false
            };
        }
    }
}

