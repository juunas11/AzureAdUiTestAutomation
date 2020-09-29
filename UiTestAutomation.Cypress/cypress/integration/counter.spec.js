/// <reference types="cypress" />

context("counter", () => {
  beforeEach(function () {
    cy.login().get("[data-cy=nav-counter]").click();
  });

  it("increments a counter", function () {
    cy.get("[data-cy=btn-counter-inc]").click();
    cy.get("[data-cy=counter-count]").should("contain.text", "1");

    cy.get("[data-cy=btn-counter-inc]").click();
    cy.get("[data-cy=counter-count]").should("contain.text", "2");
  });
});
