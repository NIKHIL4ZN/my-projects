using SmartX.Core.DTOs;
using SmartX.Core.Enums;
using SmartX.Core.Validation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.DataStructures
{
    public class DeploymentTreeValidatorTests
    {
        private readonly DeploymentTreeValidator
            _validator = new();

        [Fact]
        public void Validate_AcceptsSafeNestedTree()
        {
            var root = new DeploymentNodeRequest
            {
                Name = "Facility A",
                NodeType =
                    DeploymentNodeType.Facility,
                Children =
                [
                    new DeploymentNodeRequest
                {
                    Name = "Zone 1",
                    NodeType =
                        DeploymentNodeType.Zone,
                    Children =
                    [
                        new DeploymentNodeRequest
                        {
                            Name = "Gateway 1",
                            NodeType =
                                DeploymentNodeType.Gateway,
                            Children =
                            [
                                new DeploymentNodeRequest
                                {
                                    Name = "Sensor A",
                                    NodeType =
                                        DeploymentNodeType.Sensor,
                                    SensorMacAddress =
                                        "AA:BB:CC:DD:EE:01"
                                }
                            ]
                        }
                    ]
                }
                ]
            };

            var result = _validator.Validate(root);

            Assert.True(result.IsValid);
            Assert.Equal(4, result.NodesVisited);
            Assert.Equal(
                3,
                result.MaximumDepthReached);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_RejectsSensorInsideFacility()
        {
            var root = new DeploymentNodeRequest
            {
                Name = "Facility A",
                NodeType =
                    DeploymentNodeType.Facility,
                Children =
                [
                    new DeploymentNodeRequest
                {
                    Name = "Unsafe Sensor",
                    NodeType =
                        DeploymentNodeType.Sensor,
                    SensorMacAddress = "INVALID"
                }
                ]
            };

            var result = _validator.Validate(root);

            Assert.False(result.IsValid);

            Assert.Contains(
                result.Errors,
                error => error.Contains(
                    "cannot be placed directly"));
        }

        [Fact]
        public void Validate_RejectsDuplicateChildNames()
        {
            var root = new DeploymentNodeRequest
            {
                Name = "Facility A",
                NodeType =
                    DeploymentNodeType.Facility,
                Children =
                [
                    new DeploymentNodeRequest
                {
                    Name = "Zone A",
                    NodeType =
                        DeploymentNodeType.Zone
                },
                new DeploymentNodeRequest
                {
                    Name = "zone a",
                    NodeType =
                        DeploymentNodeType.Zone
                }
                ]
            };

            var result = _validator.Validate(root);

            Assert.False(result.IsValid);

            Assert.Contains(
                result.Errors,
                error => error.Contains(
                    "duplicate child name"));
        }
    }
}
