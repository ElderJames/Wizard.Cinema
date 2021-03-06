﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Infrastructures;
using Infrastructures.Attributes;
using Infrastructures.Encrypt.Extensions;
using Microsoft.Extensions.Logging;
using Wizard.Cinema.Application.DTOs.Request.Wizards;
using Wizard.Cinema.Application.Services.Dto.Request;
using Wizard.Cinema.Application.Services.Dto.Request.Wizards;
using Wizard.Cinema.Application.Services.Dto.Response;
using Wizard.Cinema.Domain.Ministry;
using Wizard.Cinema.Domain.Wizard;
using Wizard.Cinema.Domain.Wizard.EnumTypes;
using Wizard.Cinema.QueryServices;
using Wizard.Cinema.QueryServices.DTOs;
using Wizard.Cinema.QueryServices.DTOs.Wizards;
using LoggerExtensions = Microsoft.Extensions.Logging.LoggerExtensions;

namespace Wizard.Cinema.Application.Services
{
    [Service]
    public class WizardService : IWizardService
    {
        private readonly IWizardRepository _wizardRepository;
        private readonly IWizardQueryService _wizardQueryService;
        private readonly IWizardProfileRepository _wizardPRofileRepository;
        private readonly IWizardProfileQueryService _wizardProfileQueryService;
        private readonly ITransactionRepository _transactionRepository;

        private readonly IDivisionQueryService _divisionQueryService;
        private readonly IDivisionRepository _divisionRepository;
        private readonly ILogger<WizardService> _logger;

        public WizardService(IWizardRepository wizardRepository,
            IWizardQueryService wizardQueryService,
            IWizardProfileRepository wizardPRofileRepository,
            IWizardProfileQueryService wizardProfileQueryService,
            ITransactionRepository transactionRepository,
            ILogger<WizardService> logger,
            IDivisionQueryService divisionQueryService,
            IDivisionRepository divisionRepository)
        {
            this._wizardRepository = wizardRepository;
            this._wizardQueryService = wizardQueryService;
            this._wizardPRofileRepository = wizardPRofileRepository;
            this._wizardProfileQueryService = wizardProfileQueryService;
            this._transactionRepository = transactionRepository;
            this._logger = logger;
            this._divisionQueryService = divisionQueryService;
            this._divisionRepository = divisionRepository;
        }

        public ApiResult<bool> Register(RegisterWizardReqs request)
        {
            try
            {
                if (_wizardQueryService.Query(request.Account) != null)
                    return new ApiResult<bool>(ResultStatus.FAIL, "用户名已存在");

                if (_wizardQueryService.QueryByEmail(request.Email) != null)
                    return new ApiResult<bool>(ResultStatus.FAIL, "邮箱已被注册");

                long wizardId = NewId.GenerateId();

                var wizard = new Wizards(wizardId, request.Account, request.Email, request.Passward);

                _transactionRepository.UseTransaction(IsolationLevel.ReadUncommitted, () =>
                {
                    if (_wizardRepository.Create(wizard) <= 0)
                        throw new Exception("保存时发生异常，请稍后再试(1)");

                    if (_wizardPRofileRepository.Insert(wizard.Profile) <= 0)
                        throw new Exception("保存时发生异常，请稍后再试(2)");
                });

                return new ApiResult<bool>(ResultStatus.SUCCESS, true);
            }
            catch (Exception ex)
            {
                LoggerExtensions.LogError(_logger, "注册时异常", ex);
                return new ApiResult<bool>(ResultStatus.EXCEPTION, ex.Message);
            }
        }

        public ApiResult<WizardResp> GetWizard(string account, string passward)
        {
            WizardInfo wizard = _wizardQueryService.Query(account, passward.ToMd5());
            if (wizard == null)
                return new ApiResult<WizardResp>(ResultStatus.FAIL, "用户不能存在或密码不正确");

            return new ApiResult<WizardResp>(ResultStatus.SUCCESS, Mapper.Map<WizardInfo, WizardResp>(wizard));
        }

        public ApiResult<IEnumerable<WizardResp>> GetWizards(long[] wizardIds)
        {
            if (wizardIds.IsNullOrEmpty())
                return new ApiResult<IEnumerable<WizardResp>>(ResultStatus.SUCCESS, Enumerable.Empty<WizardResp>());

            IEnumerable<WizardInfo> wizard = _wizardQueryService.Query(wizardIds);
            if (wizard == null)
                return new ApiResult<IEnumerable<WizardResp>>(ResultStatus.FAIL, "用户不能存在或密码不正确");

            return new ApiResult<IEnumerable<WizardResp>>(ResultStatus.SUCCESS, Mapper.Map<WizardInfo, WizardResp>(wizard));
        }

        public ApiResult<WizardResp> GetWizard(long wizardId)
        {
            WizardInfo wizard = _wizardQueryService.Query(wizardId);
            if (wizard == null)
                return new ApiResult<WizardResp>(ResultStatus.FAIL, "用户不能存在或密码不正确");

            return new ApiResult<WizardResp>(ResultStatus.SUCCESS, Mapper.Map<WizardInfo, WizardResp>(wizard));
        }

        public ApiResult<PagedData<WizardResp>> Search(SearchWizardReqs search)
        {
            PagedData<WizardInfo> wizards = _wizardQueryService.QueryPaged(Mapper.Map<SearchWizardReqs, WizardSearch>(search));
            return new ApiResult<PagedData<WizardResp>>(ResultStatus.SUCCESS, new PagedData<WizardResp>()
            {
                PageSize = wizards.PageSize,
                PageNow = wizards.PageNow,
                TotalCount = wizards.TotalCount,
                Records = wizards.Records.Select(Mapper.Map<WizardInfo, WizardResp>)
            });
        }

        public ApiResult<ProfileResp> GetProfile(long wizardId)
        {
            if (wizardId <= 0)
                return new ApiResult<ProfileResp>(ResultStatus.FAIL, "请选择正确的巫师");

            ProfileInfo profile = _wizardProfileQueryService.Query(wizardId);
            if (profile == null)
                return new ApiResult<ProfileResp>(ResultStatus.FAIL, "巫师不存在");

            return new ApiResult<ProfileResp>(ResultStatus.SUCCESS, Mapper.Map<ProfileInfo, ProfileResp>(profile));
        }

        public ApiResult<ProfileResp> ChangeProfile(ChangeProfilepReqs request)
        {
            WizardProfiles profile = _wizardPRofileRepository.Query(request.WizardId);
            if (profile == null)
                return new ApiResult<ProfileResp>(ResultStatus.FAIL, "巫师不存在");

            profile.Change(request.NickName, request.PortraitUrl, request.Mobile, (Gender)request.Gender, request.Birthday, request.Slogan, (Houses)request.House);

            if (_wizardPRofileRepository.Update(profile) <= 0)
                return new ApiResult<ProfileResp>(ResultStatus.FAIL, "保存时出错，请稍后再试");

            return new ApiResult<ProfileResp>(ResultStatus.SUCCESS, Mapper.Map<WizardProfiles, ProfileResp>(profile));
        }

        public ApiResult<bool> CreateWizard(CreateWizardReqs request)
        {
            if (string.IsNullOrEmpty(request.Account))
                return new ApiResult<bool>(ResultStatus.FAIL, "巫师名未提交");

            if (_wizardRepository.Query(request.Account) != null)
                return new ApiResult<bool>(ResultStatus.FAIL, "巫师名重复了");

            long wizardId = NewId.GenerateId();
            var wizard = new Wizards(wizardId, request.Account, request.Passward, request.DivisionId, request.CreatorId);

            if (_wizardRepository.Create(wizard) <= 0)
                return new ApiResult<bool>(ResultStatus.FAIL, "保存时异常，请稍后再试");

            return new ApiResult<bool>(ResultStatus.SUCCESS, true);
        }

        public ApiResult<bool> UpdateWizard(UpdateWizardReqs request)
        {
            Wizards wizard = _wizardRepository.Query(request.WizardId);
            if (wizard == null)
                return new ApiResult<bool>(ResultStatus.FAIL, "找不到这名巫师的记录");

            Divisions division = _divisionRepository.Query(request.DivisionId);
            if (division == null)
                return new ApiResult<bool>(ResultStatus.FAIL, "所选分部不存在");

            wizard.Change(request.Account, request.DivisionId, request.Passward);

            if (_wizardRepository.Update(wizard) <= 0)
                return new ApiResult<bool>(ResultStatus.FAIL, "没有更新任何信息");

            return new ApiResult<bool>(ResultStatus.SUCCESS, true);
        }
    }
}
