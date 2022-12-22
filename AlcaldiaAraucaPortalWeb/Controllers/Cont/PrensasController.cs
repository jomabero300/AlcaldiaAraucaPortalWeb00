﻿using AlcaldiaAraucaPortalWeb.Data.Entities.Alar;
using AlcaldiaAraucaPortalWeb.Data.Entities.Cont;
using AlcaldiaAraucaPortalWeb.Data.Entities.Gene;
using AlcaldiaAraucaPortalWeb.Helpers.Alar;
using AlcaldiaAraucaPortalWeb.Helpers.Cont;
using AlcaldiaAraucaPortalWeb.Helpers.Gene;
using AlcaldiaAraucaPortalWeb.Helpers.Subs;
using AlcaldiaAraucaPortalWeb.Models.Gene;
using AlcaldiaAraucaPortalWeb.Models.ModelsViewCont;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace AlcaldiaAraucaPortalWeb.Controllers.Cont
{
    [Authorize(Roles = "Prensa,Administrador")]
    public class PrensasController : Controller
    {
        private readonly IPqrsStrategicLineHelper _strategicLineHelper;
        private readonly IPqrsStrategicLineSectorHelper _strategicLineSectorHelper;
        private readonly IFolderStrategicLineasHelper _folderStrategicLineasHelper;
        private readonly IImageHelper _imageHelper;
        private readonly IStateHelper _stateHelper;
        private readonly ISubscriberSectorHelper _subscriberSectorHelper;
        private readonly IPqrsUserStrategicLineHelper _userStrategicLineHelper;
        private readonly IContentHelper _contentHelper;
        private readonly IUserHelper _userHelper;
        private readonly IUtilitiesHelper _utilitiesHelper;

        public PrensasController(
            IPqrsStrategicLineHelper strategicLineHelper,
            IPqrsStrategicLineSectorHelper strategicLineSectorHelper,
            IFolderStrategicLineasHelper folderStrategicLineasHelper,
            IImageHelper imageHelper, IStateHelper stateHelper,
            ISubscriberSectorHelper subscriberSectorHelper,
            IPqrsUserStrategicLineHelper userStrategicLineHelper,
            IContentHelper contentHelper, IUserHelper userHelper, 
            IUtilitiesHelper utilitiesHelper)
        {
            _strategicLineHelper = strategicLineHelper;
            _strategicLineSectorHelper = strategicLineSectorHelper;
            _folderStrategicLineasHelper = folderStrategicLineasHelper;
            _imageHelper = imageHelper;
            _stateHelper = stateHelper;
            _subscriberSectorHelper = subscriberSectorHelper;
            _userStrategicLineHelper = userStrategicLineHelper;
            _contentHelper = contentHelper;
            _userHelper = userHelper;
            _utilitiesHelper = utilitiesHelper;
        }

        public IActionResult IndexPrueba()
        {
            return View();
        }

        public async Task<IActionResult> Index()
        {
            List<Content> model = await _contentHelper.ListReporterAsync();

            ViewBag.LineName = "Prensa";

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {

            ViewData["PqrsStrategicLineId"] = new SelectList(await _strategicLineHelper.PqrsStrategicLineComboPrenAsync(), "PqrsStrategicLineId", "PqrsStrategicLineName");

            ViewData["PqrsStrategicLineSectorId"] = new SelectList(await _strategicLineSectorHelper.ComboAsync(0), "PqrsStrategicLineSectorId", "PqrsStrategicLineSectorName");

            ContentModelsViewContPren model = new ContentModelsViewContPren
            {
                ContentDetails = new List<ContentDetailModelsViewCont>()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ContentModelsViewContPren model)
        {
            var userId = await _userHelper.GetUserAsync(User.Identity.Name);
                
            if (ModelState.IsValid)
            {
                ContentModelsViewCont modelAdd = new ContentModelsViewCont()
                {
                    PqrsStrategicLineSectorId = model.PqrsStrategicLineSectorId,
                    UserId = userId.Id,
                    ContentDate = DateTime.Now,
                    ContentTitle = model.ContentTitle,
                    ContentText = model.ContentText,
                    ContentUrlImg = model.ContentUrlImg,
                    StateId = model.StateId,
                    ContentDetails = model.ContentDetails.Select(c => new ContentDetailModelsViewCont()
                    {
                        ContentTitle = c.ContentTitle,
                        ContentText = c.ContentText,
                        ContentUrlImg = c.ContentUrlImg,
                        isEsta=c.isEsta
                    }).ToList()
                };

                Response response = await _contentHelper.AddAsync(modelAdd);
                
                if(response.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, response.Message);
            }

            var strategiaLineaId = await _userStrategicLineHelper.PqrsStrategicLineBIdAsync(userId.Id);

            ViewData["PqrsStrategicLineSectorId"] = new SelectList(await _strategicLineSectorHelper.ComboAsync(strategiaLineaId.PqrsStrategicLineId), "PqrsStrategicLineSectorId", "PqrsStrategicLineSectorName", model.PqrsStrategicLineSectorId);

            return View(model);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Content content = await _contentHelper.ByIdAsync((int)id);

            if (content == null)
            {
                return NotFound();
            }

            ContentEditViewModel model = new ContentEditViewModel()
            {
                ContentId = content.ContentId,
                UserId = content.ApplicationUser.Email,
                ContentDate = content.ContentDate,
                StateId = content.StateId,
                PqrsStrategicLineSectorId = content.PqrsStrategicLineSectorId,
                ContentTitle = content.ContentTitle,
                ContentText = content.ContentText,
                ContentUrlImg1 = content.ContentUrlImg,
                ContentDetails = content.ContentDetails,
            };


            PqrsStrategicLineSector strategiaLineaId = await _strategicLineSectorHelper.ByIdAsync(content.PqrsStrategicLineSectorId);

            PqrsStrategicLine strategicLine = await _userStrategicLineHelper.PqrsStrategicLineBIdAsync(strategiaLineaId.PqrsStrategicLineId);

            ViewData["PqrsStrategicLineSectorId"] = new SelectList(await _strategicLineSectorHelper.ComboAsync(strategiaLineaId.PqrsStrategicLineId), "PqrsStrategicLineSectorId", "PqrsStrategicLineSectorName");

            List<State> estados = (await _stateHelper.StateComboAsync("G")).Where(s => s.StateName.Equals("Activo") || s.StateName.Equals("Previo")).ToList();

            estados.Where(s => s.StateName.Equals("Activo")).Select(s => { s.StateName = "Publicar"; return s.StateName; }).ToList();

            ViewData["StateId"] = new SelectList(estados, "StateId", "StateName");

            ViewBag.LineName = strategicLine.PqrsStrategicLineName;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContentEditViewModel model)
        {
            if (id != model.ContentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {

                Response response = await _contentHelper.UpdateAsync(model);

                if (response.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, response.Message);
            }

            PqrsStrategicLineSector strategiaLineaId = await _strategicLineSectorHelper.ByIdAsync(model.PqrsStrategicLineSectorId);

            PqrsStrategicLine strategicLine = await _userStrategicLineHelper.PqrsStrategicLineBIdAsync(strategiaLineaId.PqrsStrategicLineId);

            ViewData["PqrsStrategicLineSectorId"] = new SelectList(await _strategicLineSectorHelper.ComboAsync(strategiaLineaId.PqrsStrategicLineId), "PqrsStrategicLineSectorId", "PqrsStrategicLineSectorName", model.PqrsStrategicLineSectorId);


            List<State> estados = (await _stateHelper.StateComboAsync("G")).Where(s => s.StateName.Equals("Activo") || s.StateName.Equals("Previo")).ToList();

            estados.Where(s => s.StateName.Equals("Activo")).Select(s => { s.StateName = "Publicar"; return s.StateName; }).ToList();

            ViewData["StateId"] = new SelectList(estados, "StateId", "StateName");


            return View(model);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Content content = await _contentHelper.ByIdAsync((int)id);

            if (content == null)
            {
                return NotFound();
            }

            return View(content);
        }

        // POST: Contents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Response response=await _contentHelper.DeleteAsync(id);

            if(response.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, response.Message);

            Content content = await _contentHelper.ByIdAsync((int)id);

            return View(content);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDetails(int id)
        {

            try
            {
                ContentDetail model = await _contentHelper.DetailsIdAsync(id);

                Content content = await _contentHelper.ByIdAsync(model.ContentId);

                var response = await _contentHelper.DeleteDetailsAsync(id);

                //TODO: Eliminar las imagenes
                if (response.Succeeded)
                {
                    PqrsStrategicLineSector pqrsStrategicLine = await _strategicLineSectorHelper.ByIdAsync(content.PqrsStrategicLineSectorId);

                    string folder = await _folderStrategicLineasHelper.FolderPathAsync(pqrsStrategicLine.PqrsStrategicLineId, content.PqrsStrategicLineSectorId);

                    string responsE = await _imageHelper.DeleteImageAsync(model.ContentUrlImg, folder);
                }

                return Json(new { status = true });
            }
            catch (System.Exception ex)
            {
                string ltmensaje = string.Empty;
                if (ex.InnerException.Message.Contains("IX_"))
                {
                    ltmensaje = "El registro ya existe..";
                }
                else if (ex.InnerException.Message.Contains("REFERENCE"))
                {
                    ltmensaje = "El registro no se puede eliminar porque tiene registros relacionados";
                }
                else
                {
                    ltmensaje = ex.Message;
                }

                return Json(new { status = false, message = ltmensaje });
            }
        }
        [HttpPost]
        public async Task<IActionResult> AddContentDetalle(int id, string Title, string ContentText, IFormFile file, string fileUrl)
        {
            //TODO: Agregar las imagenes
            Content lineaSector = await _contentHelper.ByIdAsync(id);
            string imgUrl = fileUrl;

            if (Title.Contains("http"))
            {
                Title = _utilitiesHelper.ConvertToTextInLik(Title);
            }
            if (ContentText.Contains("http"))
            {
                ContentText = _utilitiesHelper.ConvertToTextInLik(ContentText);
            }

            if (file != null)
            {
                PqrsStrategicLineSector pqrsStrategicLine = await _strategicLineSectorHelper.ByIdAsync(lineaSector.PqrsStrategicLineSectorId);

                string folder = await _folderStrategicLineasHelper.FolderPathAsync(pqrsStrategicLine.PqrsStrategicLineId, lineaSector.PqrsStrategicLineSectorId);

                imgUrl = await _imageHelper.UploadImageAsync(file, folder);
            }

            ContentDetail detalle = new ContentDetail
            {
                ContentId = id,
                ContentTitle = Title,
                ContentText = ContentText,
                ContentUrlImg = imgUrl,
                ContentDate = DateTime.Now,
                StateId = 1
            };

            Response response = await _contentHelper.AddEditDetailAsync(detalle);


            return Json(new { status = response.Succeeded });
        }

        public async Task<IActionResult> UpdateContentDetalle(int id, string Title, string ContentText, IFormFile file, string fileUrl, int idDetail, string UrlImgOld, DateTime ContentDetailDate)
        {
            ContentDetail detalle = await _contentHelper.DetailsIdAsync(idDetail);

            Content lineaSector = await _contentHelper.ByIdAsync(id);

            PqrsStrategicLineSector linea = await _strategicLineSectorHelper.ByIdAsync(lineaSector.PqrsStrategicLineSectorId);

            string response = string.Empty;

            response = !string.IsNullOrWhiteSpace(fileUrl) ? fileUrl : UrlImgOld;

            if (Title.Contains("http"))
            {
                Title = _utilitiesHelper.ConvertToTextInLik(Title);
            }
            if (ContentText.Contains("http"))
            {
                ContentText = _utilitiesHelper.ConvertToTextInLik(ContentText);
            }

            string folder = file != null || response != UrlImgOld ? await _folderStrategicLineasHelper.FolderPathAsync(linea.PqrsStrategicLineId, lineaSector.PqrsStrategicLineSectorId) : "";

            if (file != null)
            {
                response = await _imageHelper.UploadImageAsync(file, folder);
            }
            if (response != UrlImgOld)
            {
                await _imageHelper.DeleteImageAsync(UrlImgOld, folder);
            }

            detalle.ContentTitle = Title;
            detalle.ContentText = ContentText;
            detalle.ContentUrlImg = response;

            Response responsed = await _contentHelper.AddEditDetailAsync(detalle);

            if (!responsed.Succeeded)
            {
                ModelState.AddModelError(string.Empty, responsed.Message);
            }

            return Json(new { status = responsed.Succeeded });

        }


        public async Task<JsonResult> getSector(int Id)
        {
            var strategicLine = await _strategicLineSectorHelper.ComboAsync(Id);

            return Json(strategicLine);
        }
    }
}