document.addEventListener("DOMContentLoaded", function () {
    const favoriteButton = document.getElementById("favorite-button");

    if (favoriteButton) {
        favoriteButton.addEventListener("click", function () {
            const productId = this.getAttribute("data-product-id");

            fetch("/Favorites/ToggleFavorite", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({ productId: productId })
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        if (data.isFavorite) {
                            favoriteButton.textContent = "Удалить из избранного";
                            favoriteButton.classList.add("favorite-active");
                        } else {
                            favoriteButton.textContent = "Добавить в избранное";
                            favoriteButton.classList.remove("favorite-active");
                        }
                    } else {
                        alert("Ошибка: " + data.message);
                    }
                })
                .catch(error => console.error("Ошибка при изменении избранного:", error));
        });
    }
});
