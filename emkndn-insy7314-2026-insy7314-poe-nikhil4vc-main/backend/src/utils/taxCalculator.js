
const TAX_RATE = 0.15;

function calculateEstimatedTax(totalIncome) {
    const income = Number(totalIncome) || 0;

    const estimatedTax = income * TAX_RATE;

    const estimatedNetIncome =
        income - estimatedTax;

    const effectiveTaxRate =
        income > 0
            ? TAX_RATE * 100
            : 0;

    return {
        grossIncome: Number(
            income.toFixed(2)
        ),

        estimatedTax: Number(
            estimatedTax.toFixed(2)
        ),

        estimatedNetIncome: Number(
            estimatedNetIncome.toFixed(2)
        ),

        effectiveTaxRate: Number(
            effectiveTaxRate.toFixed(2)
        )
    };
}

module.exports = {
    calculateEstimatedTax
};
