(function () {

    const recipeData = {
        vanilla: {
            id: 'vanilla',
            title: 'Madagascar Vanilla',
            badge: 'Free Access',
            prepTime: '25 mins',
            yield: '1 Quart',
            value: '$3.99 / scoop',
            summary: 'Learn how to make our timeless classic.',
            image: 'https://images.unsplash.com/photo-1570145820259-b5b80c5c8bd6?q=80&w=600&auto=format&fit=crop',
            ingredients: [
                '2 cups Heavy whipping cream',
                '1 cup Whole milk',
                '3/4 cup Sugar'
            ],
            instructions: [
                'Heat milk and vanilla.',
                'Temper egg yolks.',
                'Cook until thick.'
            ],
            lockedLines: [
                'Chill for 4 hours.',
                'Churn until creamy.'
            ],
            lockTitle: 'Unlock Full Recipe',
            lockDesc: 'Create a free account to continue.'
        }
    };

    const cardsGrid = document.getElementById("cardsGrid");
    const detailView = document.getElementById("detailView");
    const detailContent = document.getElementById("detailContent");
    const backBtn = document.getElementById("backToCards");

    // Agar ye page recipe page nahi hai to code yahin stop ho jaye
    if (!cardsGrid || !detailView || !detailContent || !backBtn) {
        return;
    }

    function renderDetail(recipeKey) {

        const recipe = recipeData[recipeKey];

        if (!recipe) return;

        detailContent.innerHTML = `
            <div class="recipe-header-block">
                <div class="recipe-image-side">
                    <img src="${recipe.image}" class="recipe-main-img" alt="">
                </div>

                <div class="recipe-info-side">
                    <span class="recipe-badge">${recipe.badge}</span>

                    <h2>${recipe.title}</h2>

                    <p>${recipe.summary}</p>

                    <ul>
                        ${recipe.ingredients.map(x => `<li>${x}</li>`).join("")}
                    </ul>

                    <ol>
                        ${recipe.instructions.map(x => `<li>${x}</li>`).join("")}
                    </ol>

                    <div class="locked-steps-preview">
                        ${recipe.lockedLines.map(x => `<div class="blur-text-line">${x}</div>`).join("")}

                        <div class="recipe-lock-overlay">
                            <h4>${recipe.lockTitle}</h4>
                            <p>${recipe.lockDesc}</p>
                            <button class="btn-unlock-recipe">
                                Create Free Account
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    function showDetail(recipeKey) {

        renderDetail(recipeKey);

        cardsGrid.style.display = "none";

        detailView.classList.add("active");

        detailView.scrollIntoView({
            behavior: "smooth"
        });
    }

    function showCards() {

        detailView.classList.remove("active");

        cardsGrid.style.display = "grid";

        cardsGrid.scrollIntoView({
            behavior: "smooth"
        });
    }

    document.querySelectorAll(".btn-detail").forEach(btn => {

        btn.addEventListener("click", function () {

            const recipe = this.dataset.recipe;

            if (recipe) {

                showDetail(recipe);

            }

        });

    });

    backBtn.addEventListener("click", showCards);

    document.addEventListener("click", function (e) {

        if (e.target.classList.contains("btn-unlock-recipe")) {

            alert("Redirect to Register Page");

        }

    });

})();