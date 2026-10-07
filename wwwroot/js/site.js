// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


document.addEventListener("DOMContentLoaded", function () {

    /*
     * Automatically hide success/error alerts
     * after 6 seconds.
     */

    const alerts =
        document.querySelectorAll(".alert");

    alerts.forEach(function (alert) {

        setTimeout(function () {

            const closeButton =
                alert.querySelector(".btn-close");

            if (closeButton) {
                closeButton.click();
            }

        }, 6000);

    });

});
