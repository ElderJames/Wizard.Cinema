﻿using Infrastructures;
using Wizard.Cinema.Application.DTOs.Request.Session;
using Wizard.Cinema.Application.DTOs.Response;

namespace Wizard.Cinema.Application.Services
{
    public interface ISessionService
    {
        ApiResult<bool> Create(CreateSessionReqs request);

        ApiResult<bool> Change(UpdateSessionReqs request);

        ApiResult<SessionResp> GetSession(long sessionId);

        ApiResult<PagedData<SessionResp>> SearchSession(SearchSessionReqs search);
    }
}