FILES = *.tex *.bib ITUFrontPage.pdf */*.tex */images/*.pdf

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
	rm -rf *.aux *.blg *.bbl *.log *.out *.tdo *.synctex.gz *.toc DistributedConsensus.pdf
	rm -rf */*.aux
