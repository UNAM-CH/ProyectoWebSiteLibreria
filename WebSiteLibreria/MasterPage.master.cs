﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using Unam.CoHu.Libreria.Controller.Security;
using Unam.CoHu.Libreria.Controller.Web;

public partial class MasterPage : System.Web.UI.MasterPage
{
    private const string AntiXsrfTokenKey = "__AntiXsrfToken";
    private const string AntiXsrfUserNameKey = "__AntiXsrfUserName";
    private string _antiXsrfTokenValue;

    protected void Page_Init(object sender, EventArgs e)
    {
        // The code below helps to protect against XSRF attacks
        var requestCookie = Request.Cookies[AntiXsrfTokenKey];
        Guid requestCookieGuidValue;
        if (requestCookie != null && Guid.TryParse(requestCookie.Value, out requestCookieGuidValue))
        {
            // Use the Anti-XSRF token from the cookie
            _antiXsrfTokenValue = requestCookie.Value;
            Page.ViewStateUserKey = _antiXsrfTokenValue;
        }
        else
        {
            // Generate a new Anti-XSRF token and save to the cookie
            _antiXsrfTokenValue = Guid.NewGuid().ToString("N");
            Page.ViewStateUserKey = _antiXsrfTokenValue;

            var responseCookie = new HttpCookie(AntiXsrfTokenKey)
            {
                HttpOnly = true,
                Value = _antiXsrfTokenValue
            };
            if (FormsAuthentication.RequireSSL && Request.IsSecureConnection)
            {
                responseCookie.Secure = true;
            }
            Response.Cookies.Set(responseCookie);
        }

        Page.PreLoad += master_Page_PreLoad;

        ShowProtectedMenus();
    }

    protected void master_Page_PreLoad(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // Set Anti-XSRF token
            ViewState[AntiXsrfTokenKey] = Page.ViewStateUserKey;
            ViewState[AntiXsrfUserNameKey] = Context.User.Identity.Name ?? String.Empty;
        }
        else
        {
            // Resignar el ViewState cuando no exista la sesión
            if (ConfiguracionSitio.UsuarioSistema == null)
            {
                ViewState[AntiXsrfUserNameKey] = Context.User.Identity.Name ?? String.Empty;
            }

            // Validate the Anti-XSRF token
            if ((string)ViewState[AntiXsrfTokenKey] != _antiXsrfTokenValue
                || (string)ViewState[AntiXsrfUserNameKey] != (Context.User.Identity.Name ?? String.Empty))
            {
                var user = ViewState[AntiXsrfUserNameKey];
                throw new InvalidOperationException("Validation of Anti-XSRF token failed.");
            }
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    private void ShowProtectedMenus()
    {
        if (AutenticacionSitio.IsUsuarioAutenticado()) //(ConfiguracionSitio.UsuarioSistema != null)
        {
            MenuCatalogos.Visible = true;
            MenuTitulos.Visible = true;
            MenuReportes.Visible = true;
        }
        else
        {
            MenuCatalogos.Visible = false;
            MenuTitulos.Visible = false;
            MenuReportes.Visible = false;
            // Eliminar la cookie de autenticacion
            //FormsAuthentication.SignOut();
            AutenticacionSitio.TerminarSesionPrincipal(false);
        }
    }

    protected void LinkButton_Click(object sender, EventArgs e)
    {
        AutenticacionSitio.TerminarSesionPrincipal(false);
        ViewState[AntiXsrfUserNameKey] = String.Empty;
        Response.Redirect("~/");
    }
}
