﻿using AlcaldiaAraucaPortalWeb.Common;
using AlcaldiaAraucaPortalWeb.Data;
using AlcaldiaAraucaPortalWeb.Data.Entities.Alar;
using AlcaldiaAraucaPortalWeb.Data.Entities.Cont;
using AlcaldiaAraucaPortalWeb.Data.Entities.Gene;
using AlcaldiaAraucaPortalWeb.Data.Entities.Subs;
using AlcaldiaAraucaPortalWeb.Helpers.Alar;
using AlcaldiaAraucaPortalWeb.Helpers.Gene;
using AlcaldiaAraucaPortalWeb.Helpers.Subs;
using AlcaldiaAraucaPortalWeb.Models.Gene;
using AlcaldiaAraucaPortalWeb.Models.ModelsViewCont;
using Microsoft.EntityFrameworkCore;

namespace AlcaldiaAraucaPortalWeb.Helpers.Cont
{
    public class ContentHelper : IContentHelper
    {
        private readonly ApplicationDbContext _context;
        private readonly IPqrsUserStrategicLineHelper _userStrategicLineHelper;
        private readonly IFolderStrategicLineasHelper _folderStrategicLineasHelper;
        private readonly ISubscriberSectorHelper _subscriberSectorHelper;

        private readonly IImageHelper _imageHelper;
        private readonly IStateHelper _stateHelper;

        public ContentHelper(ApplicationDbContext context, IPqrsUserStrategicLineHelper userStrategicLineHelper, IFolderStrategicLineasHelper folderStrategicLineasHelper, IImageHelper imageHelper, IStateHelper stateHelper, ISubscriberSectorHelper subscriberSectorHelper)
        {
            _context = context;
            _userStrategicLineHelper = userStrategicLineHelper;
            _folderStrategicLineasHelper = folderStrategicLineasHelper;
            _imageHelper = imageHelper;
            _stateHelper = stateHelper;
            _subscriberSectorHelper = subscriberSectorHelper;
        }

        public async Task<Response> ActiveAsync(int id)
        {
            State state = await _context.States.Where(s => s.StateName == "Activo" && s.StateType=="G").FirstOrDefaultAsync();

            Content model = await _context.Contents.Where(c => c.ContentId == id).FirstOrDefaultAsync();

            model.StateId = state.StateId;

            Response response = new Response() { Succeeded = true };

            _context.Contents.Update(model);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                response.Succeeded = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<Response> AddAsync(ContentModelsViewCont model)
        {
            ApplicationUser user = await _context.Users.Where(c => c.Email == model.UserId).FirstOrDefaultAsync();

            model.ContentTitle = Utilities.StartCharacterToUpper(model.ContentTitle);

            model.ContentText = Utilities.StartCharacterToUpper(model.ContentText);

            if (model.ContentText.Contains("http"))
            {
                model.ContentText = Utilities.ConvertToTextInLik(model.ContentText);
            }

            string folder = await _folderStrategicLineasHelper.FolderPathAsync(model.PqrsStrategicLineSectorId, model.UserId);

            var path = string.Empty;

            if (model.ContentUrlImg != null)
            {
                path = await _imageHelper.UploadImageAsync(model.ContentUrlImg, folder);
            }

            int stateId = await _stateHelper.StateIdAsync("G", "Previo");

            for (int i = 0; i < model.ContentDetails.Count; i++)
            {
                if (model.ContentDetails[i].isEsta == 1)
                {
                    model.ContentDetails[i].ContentUrlImg = _folderStrategicLineasHelper.FileMove(model.ContentDetails[i].ContentUrlImg, folder);
                }
                else
                {
                    model.ContentDetails[i].ContentUrlImg = model.ContentDetails[i].ContentUrlImg;
                }

                if (model.ContentDetails[i].ContentText.Contains("http"))
                {
                    model.ContentDetails[i].ContentText = Utilities.ConvertToTextInLik(model.ContentDetails[i].ContentText);
                }
            }

            var modelAdd = new Content()
            {
                PqrsStrategicLineSectorId = model.PqrsStrategicLineSectorId,
                UserId = user.Id,
                ContentDate = DateTime.Now,
                ContentTitle = model.ContentTitle,
                ContentText = model.ContentText,
                ContentUrlImg = path,
                StateId = stateId,
                ContentDetails = model.ContentDetails.Select(c => new ContentDetail()
                {
                    ContentDate = DateTime.Now,
                    ContentTitle = c.ContentTitle,
                    ContentText = c.ContentText,
                    ContentUrlImg = c.ContentUrlImg,
                    StateId = stateId
                }).ToList()
            };

            Response response = new Response() { Succeeded = true };

            List<SubscriberSector> subscriberSector = await _subscriberSectorHelper.BySectorIdAsync(model.PqrsStrategicLineSectorId);
            _context.Contents.Add(modelAdd);

            try
            {
                await _context.SaveChangesAsync();
                if (subscriberSector != null)
                {
                    _context.SubscriberSectors.UpdateRange(subscriberSector);
                    await _context.SaveChangesAsync();
                }

            }
            catch (Exception ex)
            {
                response.Succeeded = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<Response> AddEditDetailAsync(ContentDetail model)
        {

            Response response = new Response() { Succeeded = true };

            try
            {
                if (model.ContentDetailsId == 0)
                {
                    _context.ContentDetails.Add(model);
                }
                else
                {
                    _context.ContentDetails.Update(model);
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException.Message.Contains("duplica"))
                {
                    response.Message = "Ya existe este registro";
                }
                else
                {
                    response.Message = ex.InnerException.Message;
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<Content> ByIdAsync(int contentId)
        {
            Content model = await _context.Contents
                                        .Include(c=>c.ContentDetails)
                                        .Include(c=>c.PqrsStrategicLineSector)
                                        .Include(c=>c.State)
                                        .Include(u=>u.ApplicationUser)
                                        .Where(c=>c.ContentId== contentId)
                                        .FirstOrDefaultAsync();
            return model;
        }

        public async Task<List<string>> ByImageAsync(int contentId)
        {
            Content model = await _context.Contents.Include(c => c.ContentDetails).Where(c => c.ContentId == contentId).FirstOrDefaultAsync();
            List<string> image=new List<string>();
            image.Add(model.ContentUrlImg);

            foreach (var item in model.ContentDetails)
            {
                image.Add(item.ContentUrlImg);
            }

            return image;
        }

        public async Task<Content> ByUserIdAsync(string email, int contentId)
        {
            Content model = await _context.Contents
                .Include(c => c.ApplicationUser)
                .Include(c => c.PqrsStrategicLineSector)
                .Include(c => c.State)
                .FirstOrDefaultAsync(m => m.ContentId == contentId && m.ApplicationUser.Email == email);

            return model;
        }

        public async Task<Response> DeleteAsync(int id)
        {
            Response response = new Response() { Succeeded = true };

            //State stateActivo = await _context.States.Where(s => s.StateName == "Activo" && s.StateType == "G").FirstOrDefaultAsync();

            Content model = await _context.Contents
                                        .Include(c => c.ContentDetails)
                                        .Where(c => c.ContentId == id)
                                        .FirstOrDefaultAsync();
            //if (model.StateId == stateActivo.StateId)
            //{
            //    State stateInactivo = await _context.States.Where(s => s.StateName == "Inactivo" && s.StateType == "G").FirstOrDefaultAsync();

            //    model.StateId = stateInactivo.StateId;

            //    _context.Contents.Update(model);
            //}
            //else
            //{
            //    _context.Contents.Remove(model);
            //}

            _context.Contents.Remove(model);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                response.Succeeded = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<Response> DeleteDetailsAsync(int id)
        {
            var response = new Response() { Succeeded = true };

            var model = await _context.ContentDetails.Where(a => a.ContentDetailsId == id).FirstOrDefaultAsync();

            try
            {
                _context.ContentDetails.Remove(model);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                response.Succeeded = false;
                response.Message = ex.Message.Contains("REFERENCE") ? "No se puede borrar la categoría porque tiene registros relacionados" : ex.Message;
            }

            return response;

        }

        public async Task<ContentDetail> DetailsIdAsync(int ContentDetailsId)
        {
            ContentDetail model = await _context.ContentDetails.FindAsync(ContentDetailsId);

            return model;
        }

        public async Task<Response> InactiveAsync(int id)
        {
            State state = await _context.States.Where(s => s.StateName == "Inactivo" && s.StateType == "G").FirstOrDefaultAsync();

            Content model = await _context.Contents.Where(c => c.ContentId == id).FirstOrDefaultAsync();

            model.StateId = state.StateId;

            Response response = new Response() { Succeeded = true };

            _context.Contents.Update(model);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                response.Succeeded = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<List<Content>> ListAsync(int SectorId)
        {
            State estate = await _context.States.Where(s => s.StateName.Equals("Activo")).FirstOrDefaultAsync();

            List<Content> model = await _context.Contents.Where(c => c.PqrsStrategicLineSectorId == SectorId && c.StateId == estate.StateId).ToListAsync();

            return model.OrderByDescending(m => m.ContentDate).ToList();
        }

        public async Task<List<ContentDetail>> ListDetailsAsync(int contentId)
        {
            var model = await _context.ContentDetails
                                      .Where(c => c.ContentId == contentId)
                                      .OrderBy(c => c.ContentDetailsId)
                                      .ToListAsync();
            return model;

        }

        public async Task<List<Content>> ListReporterAsync()
        {
            List<Content> model = await
                _context.Contents
                                        .Include(c => c.ApplicationUser)
                                        .Include(c => c.PqrsStrategicLineSector)
                                        .Include(c => c.State)
                                        .Where(c => c.State.StateName == "Previo")
                                        .OrderByDescending(x => x.ContentId).ToListAsync();
            return model;
        }

        public async Task<List<FilterViewModel>> ListTitleAsync(string title)
        {
            List<Content> modelConten = await _context.Contents
                                          .Where(x =>
                                                 x.ContentTitle.ToUpper().Contains(title) ||
                                                 x.ContentText.ToUpper().Contains(title)).ToListAsync();

            var modelProfes = ((from a in _context.Affiliates
                                where a.Name.ToUpper().Contains(title)
                                select new { a.AffiliateId, a.Name, a.Address, a.ImagePath }).Union
                              (
                                from b in _context.Affiliates
                                join pr in _context.AffiliateProfessions on b.AffiliateId equals pr.AffiliateId
                                join p in _context.Professions on pr.ProfessionId equals p.ProfessionId
                                where p.ProfessionName.ToUpper().Contains(title)
                                select new { b.AffiliateId, b.Name, b.Address, b.ImagePath })).ToList();

            List<FilterViewModel> model = new List<FilterViewModel>();
            if (modelConten.Count > 0)
            {
                model = modelConten.Select(x => new FilterViewModel
                {
                    id = x.ContentId,
                    Name = x.ContentTitle,
                    descrition = x.ContentText,
                    ImageUrl = x.ContentUrlImg,
                    Model = "C"
                }).ToList();
            }
            if (modelProfes.Count > 0)
            {
                modelProfes.Distinct();

                foreach (var item in modelProfes)
                {

                    model.Add(new FilterViewModel
                    {
                        id = item.AffiliateId,
                        Name = item.Name,
                        descrition = item.Address,
                        ImageUrl = item.ImagePath,
                        Model = "A"
                    });
                }
            }

            return model;
        }

        public async Task<List<Content>> ListUserAsync(string email)
        {
            PqrsStrategicLine strategiaLineaId = await _userStrategicLineHelper.PqrsStrategicLineEmaildAsync(email);

            List<Content> model = _context.Contents
                                        .Include(c => c.ApplicationUser)
                                        .Include(c => c.PqrsStrategicLineSector)
                                        .Include(c => c.State)
                                        .Where(c => c.ApplicationUser.UserName == email &&
                                                    c.PqrsStrategicLineSector.PqrsStrategicLineId == strategiaLineaId.PqrsStrategicLineId)
                                        .OrderByDescending(x => x.ContentId)
                                        .ToList();

            return model;
        }

        public async Task<Response> UpdateAsync(ContentEditViewModel model)
        {
            Response response = new Response() { Succeeded = true };

            string folder = await _folderStrategicLineasHelper.FolderPathAsync(model.PqrsStrategicLineSectorId, model.UserId);

            string path = string.Empty;

            if (model.ContentUrlImg != null)
            {
                path = await _imageHelper.UploadImageAsync(model.ContentUrlImg, folder);
            }

            Content content = await _context.Contents.FindAsync(model.ContentId);
            content.PqrsStrategicLineSectorId = model.PqrsStrategicLineSectorId;
            content.ContentTitle = model.ContentTitle;
            content.ContentText = model.ContentText;
            content.ContentUrlImg = (model.ContentUrlImg != null ? path : model.ContentUrlImg1);
            content.StateId = model.StateId;

            _context.Contents.Update(content);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                response.Succeeded = false;
                response.Message = ex.Message;
            }

            return response;

        }
    }
}