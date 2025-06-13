﻿using KworkNotify.Api.Interfaces;
using KworkNotify.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KworkNotify.Api.Controllers;

[Authorize]
[Route("backup")]
[ApiController]
public class BackupController(IBackupManager backupManager) : ControllerBase, IBackupController
{
    [HttpGet("create")]
    public async Task<ActionResult> CreateBackup()
    {
        var (success, output, error) = await backupManager.CreateBackupAsync();
        if (!success)
        {
            return StatusCode(500, new { Output = output, Error = error });
        }
        return Ok(new { Output = output });
    }
    [HttpGet("send")]
    public async Task<ActionResult<List<string>>> SendBackup()
    {
        var (success, sentFiles, error) = await backupManager.SendBackupsAsync();
        return success ? Ok(sentFiles) : StatusCode(500, error);
    }
}