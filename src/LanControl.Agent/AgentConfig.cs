using System;
using LanControl.Core.Models;

namespace LanControl.Agent
{
    public class AgentConfig : LanControl.Core.Models.AgentConfig
    {
        public new static AgentConfig Load(string filePath)
        {
            var baseCfg = LanControl.Core.Models.AgentConfig.Load(filePath);
            return new AgentConfig
            {
                AgentId = baseCfg.AgentId,
                GatewayHost = baseCfg.GatewayHost,
                GatewayPort = baseCfg.GatewayPort,
                VlanTag = baseCfg.VlanTag,
                ThumbnailIntervalMs = baseCfg.ThumbnailIntervalMs,
                LiveStreamIntervalMs = baseCfg.LiveStreamIntervalMs
            };
        }
    }
}
