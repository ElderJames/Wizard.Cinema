﻿using System.Linq;
using Wizard.Cinema.Infrastructures;
using Wizard.Cinema.Remote.Repository;
using Wizard.Cinema.Remote.Repository.Condition;
using Wizard.Cinema.Remote.Spider;
using Wizard.Cinema.Remote.Spider.Request;

namespace Wizard.Cinema.Remote.ApplicationServices
{
    public class CinemaService
    {
        private readonly RemoteSpider _remoteCall;
        private readonly ICinemaRepository _cinemaRepository;
        private readonly object _locker = new object();

        public CinemaService(RemoteSpider remoteCall, ICinemaRepository cinemaRepository)
        {
            this._remoteCall = remoteCall;
            this._cinemaRepository = cinemaRepository;
        }

        public PagedData<Models.Cinema> GetByCityId(SearchCinemaCondition condition)
        {
            if (condition.PageNow == 1)
            {
                var cinemas = _cinemaRepository.GetList(condition);
                var count = 0;
                if (cinemas.IsNullOrEmpty())
                {
                    lock (_locker)
                    {
                        cinemas = _cinemaRepository.GetList(condition);

                        if (cinemas.IsNullOrEmpty())
                        {
                            var data = _remoteCall.SendAsync(new CinemaRequest { CityId = condition.CityId }).Result;
                            cinemas = data.cinemas.Select(x => new Models.Cinema()
                            {
                                CityId = condition.CityId,
                                CinemaId = x.id,
                                Name = x.nm,
                                Address = x.addr,
                            });

                            var splitArr = cinemas.Split(20);
                            foreach (var arr in splitArr)
                                _cinemaRepository.InsertBatch(arr);
                            count = cinemas.Count();
                            cinemas = cinemas.Skip(condition.StartIndex).Take(condition.PageSize);
                        }
                    }
                }

                return new PagedData<Models.Cinema>() { PageNow = condition.PageNow, PageSize = condition.PageSize, TotalCount = count, Records = cinemas };
            }

            return _cinemaRepository.QueryPage(condition);
        }
    }
}