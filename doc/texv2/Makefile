FILES = DistributedConsensus.tex bibliography.tex bibliography.bib preamble.tex \
	ITUFrontPage.pdf 1introduction/introduction.tex 1introduction/problemdefinition.tex \
	2background/background.tex 3local/representingahistory.tex 4connect/connectinghistories.tex \
	5validation/historyindcr.tex 6orderofexecution/orderofexecution.tex \
	7previous/previousapproaches.tex 8conclusion/conclusion.tex

TEX = pdflatex -synctex=1 -interaction=nonstopmode -file-line-error > /dev/null
BIB = bibtex > /dev/null

default: DistributedConsensus.pdf

DistributedConsensus.pdf: $(FILES) DistributedConsensus.bbl DistributedConsensus.blg
	$(TEX) DistributedConsensus.tex

DistributedConsensus.bbl DistributedConsensus.blg: bibliography.bib DistributedConsensus.aux
	$(BIB) DistributedConsensus.aux
	$(TEX) DistributedConsensus.tex

DistributedConsensus.aux: $(FILES)
	$(TEX) DistributedConsensus.tex

clean:
	rm -rf *.aux *.blg *.bbl *.log *.out *.synctex.gz *.toc DistributedConsensus.pdf
	rm -rf 1introduction/*.aux 2background/*.aux 3local/*.aux 4connect/*.aux 5validation/*.aux 6orderofexecution/*.aux 7previous/*.aux 8conclusion/*.aux
